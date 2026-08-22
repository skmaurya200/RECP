using Rec_Partapgarh.Models;
using Rec_Partapgarh.Security;
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace Rec_Partapgarh.Controllers
{
    public class ManagerAccountController : Controller
    {
        private const int MaximumAttempts = 5;
        private const int LockoutMinutes = 15;
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["recpLocalDb"].ConnectionString;

        [HttpGet, AllowAnonymous]
        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
        public ActionResult Login(string returnUrl)
        {
            if (User.Identity.IsAuthenticated) return RedirectToAction("Index", "Manager");
            return View(new ManagerLoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public ActionResult Login(ManagerLoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            model.Username = (model.Username ?? string.Empty).Trim();
            var now = DateTime.UtcNow;

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction(IsolationLevel.Serializable))
                using (var command = new SqlCommand(@"SELECT ManagerUserId, Username, DisplayName, PasswordHash, PasswordSalt, HashIterations,
                                                             FailedLoginAttempts, LockoutEndUtc, IsActive
                                                      FROM dbo.tbl_ManagerUser WITH (UPDLOCK, ROWLOCK)
                                                      WHERE Username = @Username", connection, transaction))
                {
                    command.Parameters.Add("@Username", SqlDbType.NVarChar, 100).Value = model.Username;
                    int userId = 0, attempts = 0, iterations = ManagerPasswordHasher.DefaultIterations;
                    string username = null, displayName = null, hash = null, salt = null;
                    DateTime? lockoutEnd = null; bool isActive = false;
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            userId = reader.GetInt32(0); username = reader.GetString(1); displayName = reader.GetString(2);
                            hash = reader.GetString(3); salt = reader.GetString(4); iterations = reader.GetInt32(5);
                            attempts = reader.GetInt32(6); lockoutEnd = reader.IsDBNull(7) ? (DateTime?)null : reader.GetDateTime(7); isActive = reader.GetBoolean(8);
                        }
                    }

                    if (userId > 0 && lockoutEnd.HasValue && lockoutEnd.Value > now)
                    {
                        transaction.Commit();
                        var remaining = Math.Max(1, (int)Math.Ceiling((lockoutEnd.Value - now).TotalMinutes));
                        ModelState.AddModelError(string.Empty, "Account is temporarily locked. Try again in " + remaining + " minute(s).");
                        return View(model);
                    }

                    // An expired lock starts a fresh attempt window.
                    if (userId > 0 && lockoutEnd.HasValue && lockoutEnd.Value <= now)
                    {
                        attempts = 0;
                        lockoutEnd = null;
                    }

                    var validPassword = userId > 0 && isActive && ManagerPasswordHasher.Verify(model.Password, hash, salt, iterations);
                    if (!validPassword)
                    {
                        // Perform equivalent work for unknown users to reduce username timing disclosure.
                        if (userId == 0) ManagerPasswordHasher.Verify(model.Password, DummyHash, DummySalt, ManagerPasswordHasher.DefaultIterations);
                        if (userId > 0)
                        {
                            attempts++;
                            var shouldLock = attempts >= MaximumAttempts;
                            using (var update = new SqlCommand(@"UPDATE dbo.tbl_ManagerUser SET FailedLoginAttempts=@Attempts,
                                                               LockoutEndUtc=@LockoutEnd WHERE ManagerUserId=@Id", connection, transaction))
                            {
                                update.Parameters.Add("@Attempts", SqlDbType.Int).Value = attempts;
                                update.Parameters.Add("@LockoutEnd", SqlDbType.DateTime2).Value = shouldLock ? (object)now.AddMinutes(LockoutMinutes) : DBNull.Value;
                                update.Parameters.Add("@Id", SqlDbType.Int).Value = userId;
                                update.ExecuteNonQuery();
                            }
                        }
                        transaction.Commit();
                        ModelState.AddModelError(string.Empty, attempts >= MaximumAttempts ? "Account is temporarily locked for 15 minutes." : "Invalid username or password.");
                        return View(model);
                    }

                    using (var update = new SqlCommand(@"UPDATE dbo.tbl_ManagerUser SET FailedLoginAttempts=0, LockoutEndUtc=NULL,
                                                       LastLoginAtUtc=SYSUTCDATETIME() WHERE ManagerUserId=@Id", connection, transaction))
                    { update.Parameters.Add("@Id", SqlDbType.Int).Value = userId; update.ExecuteNonQuery(); }
                    transaction.Commit();
                    IssueAuthenticationCookie(username, displayName, model.RememberMe);
                    if (Url.IsLocalUrl(model.ReturnUrl)) return Redirect(model.ReturnUrl);
                    return RedirectToAction("Index", "Manager");
                }
            }
        }

        [HttpPost, Authorize, ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            Session.Clear();
            Session.Abandon();

            var expiredCookie = new HttpCookie(FormsAuthentication.FormsCookieName, string.Empty)
            {
                Expires = DateTime.UtcNow.AddYears(-1),
                HttpOnly = true,
                Secure = Request.IsSecureConnection,
                SameSite = SameSiteMode.Lax,
                Path = FormsAuthentication.FormsCookiePath
            };
            Response.Cookies.Add(expiredCookie);
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            Response.Cache.SetExpires(DateTime.UtcNow.AddYears(-1));
            return RedirectToAction("Login");
        }

        private void IssueAuthenticationCookie(string username, string displayName, bool persistent)
        {
            var issued = DateTime.Now;
            var ticket = new FormsAuthenticationTicket(1, username, issued, issued.AddMinutes(persistent ? 720 : 30), persistent, displayName, FormsAuthentication.FormsCookiePath);
            var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, FormsAuthentication.Encrypt(ticket))
            { HttpOnly = true, Secure = Request.IsSecureConnection, SameSite = SameSiteMode.Lax, Path = FormsAuthentication.FormsCookiePath };
            if (persistent) cookie.Expires = ticket.Expiration;
            Response.Cookies.Add(cookie);
        }

        private const string DummySalt = "4KkxD+ZuFylPZkSTzZfNxbkH2aNw4YzglCxLT5EdJi8=";
        private const string DummyHash = "Mb24KjSywD+Q2oRlSO7IfnWv9mYBT8vEoVHVJmH/P1Y=";
    }
}
