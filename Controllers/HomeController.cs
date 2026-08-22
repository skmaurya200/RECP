

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Web;
using System.Web.Mvc;
using System.Configuration;
using System.Data.SqlClient;
using Rec_Partapgarh.Models;

namespace RAC_GONDA.Controllers
{
    public class HomeController : Controller
    {
        Rec_Partapgarh.Models.recpEntities av = new Rec_Partapgarh.Models.recpEntities();

        public ActionResult Gallery1()
        {
            var categories = new List<PublicGalleryCategoryViewModel>();
            var lookup = new Dictionary<int, PublicGalleryCategoryViewModel>();
            using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["recpLocalDb"].ConnectionString))
            using (var command = new SqlCommand(@"SELECT c.CategoryId,c.CategoryName,g.GalleryId,g.Title,g.ImagePath
                FROM dbo.tbl_gallery_category c INNER JOIN dbo.tbl_Gallery g ON g.CategoryId=c.CategoryId
                WHERE c.IsActive=1 AND g.IsActive=1 ORDER BY g.GalleryId DESC", connection))
            {
                connection.Open(); using (var reader=command.ExecuteReader()) while(reader.Read())
                {
                    var categoryId=reader.GetInt32(0);PublicGalleryCategoryViewModel category;
                    if(!lookup.TryGetValue(categoryId,out category)){category=new PublicGalleryCategoryViewModel{CategoryId=categoryId,CategoryName=reader.GetString(1)};lookup.Add(categoryId,category);categories.Add(category);}
                    category.Images.Add(new PublicGalleryImageViewModel{GalleryId=reader.GetInt32(2),Title=reader.IsDBNull(3)?null:reader.GetString(3),ImagePath=reader.GetString(4)});
                }
            }
            return View(categories);
        }
        public ActionResult Media_Coverage()
        {
            var releases=new List<PublicPressReleaseViewModel>();
            using(var connection=new SqlConnection(ConfigurationManager.ConnectionStrings["recpLocalDb"].ConnectionString))
            using(var command=new SqlCommand("SELECT PressReleaseId,Title,ImagePath FROM dbo.tbl_PressRelease WHERE IsActive=1 ORDER BY PressReleaseId DESC",connection))
            {connection.Open();using(var reader=command.ExecuteReader())while(reader.Read())releases.Add(new PublicPressReleaseViewModel{PressReleaseId=reader.GetInt32(0),Title=reader.GetString(1),ImagePath=reader.GetString(2)});}
            return View(releases);
        }
        public ActionResult Index()
        {
            ViewBag.ManagerHomeContent = LoadManagerHomeContent();
            return View();
           
        }

        private HomeManagerContentViewModel LoadManagerHomeContent()
        {
            var model = new HomeManagerContentViewModel();
            var connectionString = ConfigurationManager.ConnectionStrings["recpLocalDb"].ConnectionString;
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand("SELECT Title,SortDescription,ImagePath FROM dbo.tbl_Slider WHERE IsActive=1 ORDER BY SliderId DESC", connection))
                using (var reader = command.ExecuteReader()) while (reader.Read()) model.Sliders.Add(new HomeSliderContent { Title=reader.GetString(0), Description=reader.IsDBNull(1)?null:reader.GetString(1), ImagePath=reader.GetString(2) });
                using (var command = new SqlCommand("SELECT NoticeType,Title,FilePath,CreatedAt FROM dbo.tbl_GeneralNotice WHERE IsActive=1 ORDER BY NoticeId DESC", connection))
                using (var reader = command.ExecuteReader()) while (reader.Read()) { var item=new HomeNoticeContent { Title=reader.GetString(1),FilePath=reader.GetString(2),CreatedAt=reader.GetDateTime(3) }; if(reader.GetString(0).Equals("Horizontal",StringComparison.OrdinalIgnoreCase))model.HorizontalNotices.Add(item);else if(reader.GetString(0).Equals("Vertical",StringComparison.OrdinalIgnoreCase))model.VerticalNotices.Add(item); }
                using (var command = new SqlCommand("SELECT EventName,Venue,EventDate,EventTime,BannerImagePath FROM dbo.tbl_Event WHERE IsActive=1 ORDER BY EventDate DESC,EventTime DESC", connection))
                using (var reader = command.ExecuteReader()) while (reader.Read()) model.Events.Add(new HomeEventContent { EventName=reader.GetString(0),Venue=reader.GetString(1),EventDate=reader.GetDateTime(2),EventTime=reader.GetTimeSpan(3),BannerImagePath=reader.IsDBNull(4)?null:reader.GetString(4) });
                using (var command = new SqlCommand("SELECT Title,SourceUrl FROM dbo.tbl_Video WHERE IsActive=1 ORDER BY VideoId DESC", connection))
                using (var reader = command.ExecuteReader()) while (reader.Read()) model.Videos.Add(new HomeVideoContent { Title=reader.GetString(0),SourceUrl=reader.GetString(1) });
                using (var command = new SqlCommand(@"SELECT g.GalleryId,c.CategoryName,g.Title,g.ImagePath FROM dbo.tbl_Gallery g
                    INNER JOIN dbo.tbl_gallery_category c ON c.CategoryId=g.CategoryId
                    WHERE g.IsActive=1 AND c.IsActive=1 ORDER BY g.GalleryId DESC", connection))
                using (var reader = command.ExecuteReader()) while (reader.Read()) model.Gallery.Add(new HomeGalleryContent { GalleryId=reader.GetInt32(0),CategoryName=reader.GetString(1),Title=reader.IsDBNull(2)?null:reader.GetString(2),ImagePath=reader.GetString(3) });
            }
            return model;
        }
        public ActionResult DepartmentDetails()
        {
            

            return View();
        }

        public ActionResult About()
        {
           
            return View();
        }
        public ActionResult Vision()
        {
          
            return View();
        }
        public ActionResult DirectorMessage()
        {
            
            return View();
        }
        public ActionResult Accreditations()
        {
            
            return View();
        }
        public ActionResult MinutesMeeting()
        {
            
            return View();
        }
        public ActionResult MemorandumAssociation()
        {
            
           
            return View();
        }
        public ActionResult G_OChange()
        {
            
         
            return View();
        }
        public ActionResult AICTEAffiliation()
        {
            
          
            return View();
        }
        public ActionResult MinutesMeetingBWCCPCFC()
        {
          
            return View();
        }
        public ActionResult AffiliationLetter()
        {
            
            return View();
        }
        public ActionResult Director()
        {
            
            return View();
        }
        public ActionResult Board_Governors()
        {
            
            return View();
        }
        public ActionResult Administration()
        {
            
            return View();
        }
        public ActionResult Council_Affairs()
        {
            
            return View();
        }
        public ActionResult Budget()
        {
            
            return View();
        }
        public ActionResult Society_Bylaws()
        {
            
         
            return View();
        }
        public ActionResult ACADEMICS_Officials()
        {
            
            return View();
        }
        public ActionResult Course_Of_study()
        {
            
            return View();
        }
        public ActionResult Syllabus()
        {
            ViewBag.NewSyllabus = LoadAcademicDocuments("Syllabus");
            return View();
        }
        public ActionResult Academic_Calendar()
        {
            
            
            return View(); // Pass the data to the view as the model
        }

        public ActionResult Fee_Structure()
        {
            
            return View();
        }
        public ActionResult Eligibility_Criteria()
        {
            
            return View();
        }
        public ActionResult List_of_Holidays()
        {
           
            return View();
        }
        public ActionResult TimeTable()
        {
            ViewBag.NewTimeTables = LoadAcademicDocuments("TimeTable");
            return View();
        }

        private List<PublicAcademicDocumentViewModel> LoadAcademicDocuments(string module)
        {
            var result=new List<PublicAcademicDocumentViewModel>();var lookup=new Dictionary<string,PublicAcademicDocumentViewModel>(StringComparer.OrdinalIgnoreCase);
            var isTimeTable=module=="TimeTable";var table=isTimeTable?"tbl_TimeTable":"tbl_Syllabus";
            var sql=isTimeTable
                ?"SELECT ISNULL(x.SessionName,''),'' AS CourseName,ISNULL(x.SemesterType,''),'' AS StudyYear,x.FilePath,COALESCE(NULLIF(x.BranchNames,''),d.DepartmentName,'-') FROM dbo.tbl_TimeTable x LEFT JOIN dbo.tbl_Department d ON d.DepartmentId=x.DepartmentId WHERE x.IsActive=1 ORDER BY x.TimeTableId DESC"
                :"SELECT '' AS SessionName,x.CourseName,'' AS SemesterType,ISNULL(x.StudyYear,''),x.FilePath,COALESCE(NULLIF(x.BranchNames,''),d.DepartmentName,'-') FROM dbo.tbl_Syllabus x LEFT JOIN dbo.tbl_Department d ON d.DepartmentId=x.DepartmentId WHERE x.IsActive=1 ORDER BY x.SyllabusId DESC";
            using(var connection=new SqlConnection(ConfigurationManager.ConnectionStrings["recpLocalDb"].ConnectionString))using(var command=new SqlCommand(sql,connection))
            {connection.Open();using(var reader=command.ExecuteReader())while(reader.Read()){var session=reader.GetString(0);var course=reader.GetString(1);var semester=reader.GetString(2);var year=reader.GetString(3);var file=reader.GetString(4);var key=session+"\u001f"+course+"\u001f"+semester+"\u001f"+year+"\u001f"+file;PublicAcademicDocumentViewModel item;if(!lookup.TryGetValue(key,out item)){item=new PublicAcademicDocumentViewModel{SessionName=session,CourseName=course,SemesterType=semester,StudyYear=year,FilePath=file};lookup.Add(key,item);result.Add(item);}var branch=reader.GetString(5);foreach(var name in branch.Split(',').Select(x=>x.Trim()).Where(x=>x.Length>0))if(!item.Branches.Contains(name,StringComparer.OrdinalIgnoreCase))item.Branches.Add(name);}}
            return result;
        }
        public ActionResult Academic_Committee_Minutes()
        {
             
            return View();
        }
        public ActionResult Association_Mentor()
        {
            
            return View();
        }
        public ActionResult CommitteeMinutesMeetings()
        {
            
            return View();
        }
        public ActionResult Overview()
        {
            
            return View();
        }
        public ActionResult Placement_Brochure()
        {
            
            return View();
        }
        public ActionResult Notification()
        {
            
            return View();
        }
        public ActionResult PastRecruiters()
        {
            
            return View();
        }
        public ActionResult TrainingPlacementOfficer()
        {
            
            return View();
        }
        public ActionResult Placement_Record()
        {
            
            return View();
        }
        public ActionResult MoU()
        {
            
            return View();
        }
        public ActionResult Centralfacility()
        {
            
            return View();
        }
        public ActionResult Scholarship()
        {
            
            return View();
        }



        public ActionResult AntiRaggingEnabled()
        {
            
            return View();
        }
        public ActionResult AntiRaggingComplaint()
        {
            
            return View();
        }
        public ActionResult GrievancePortal()
        {
            
            return View();
        }
        
        public ActionResult Training_Placement() { 
            return View(); 
        
        }




        public ActionResult CoCurricular()
        {
            
            return View();
        }
        public ActionResult Hostels()
        {
            
            return View();
        }
        public ActionResult BIC_Club()
        {
            
            return View();
        }
        public ActionResult Faculty_Directory()
        {
            var faculties = av.profiles
                              .Where(f => f.IsActive == true)
                              .OrderBy(f => f.SortOrder)
                              .ToList();

            return View(faculties);
        }
        // GET: Individual Faculty Profile
        public ActionResult FacultyProfile(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);

            var faculty = av.profiles.Find(id);

            if (faculty == null || !faculty.IsActive == true)
                return HttpNotFound(); // agar inactive ya id galat hai

            return View(faculty);
        }


        public ActionResult Research_Publications()
        {
            
            return View();
        }
        public ActionResult Research_Projects()
        {
            
            return View();
        }
        public ActionResult Research_Activities()
        {
            
            return View();
        }
        public ActionResult IQAC()
        {
            
            return View();
        }
        public ActionResult Rules()
        {
            
            return View();
        }
        public ActionResult Results()
        {
            
            return View();
        }
        public ActionResult ExaminationScheme()
        {
            
            return View();
        }
        public ActionResult ExaminationCell()
        {
            
            return View();
        }
        public ActionResult ContactDetails()
        {
            
            return View();
        }
        
        public ActionResult commingsoon()
        {
            
            return View();
        }


        // ✅ 1️⃣ Show Gallery
        public ActionResult Gallery()
        {


            return View();
        }
        public ActionResult profile()
        {
            return View();
        }
        public ActionResult CSE()
        {
            return View();
        }
        public ActionResult EE()
        {
            return View();
        }
        public ActionResult CE()
        {
            return View();
        }
        public ActionResult ME()
        {
            return View();
        }
        public ActionResult Faculty()
        {
            return View(LoadPublicFaculty());
        }

        public ActionResult Faculty_Profile(string token)
        {
            int staffId;
            if(!TryUnprotectFacultyId(token,out staffId))return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            var faculty=LoadPublicFaculty(staffId).FirstOrDefault();
            if(faculty==null)return HttpNotFound();
            return View(faculty);
        }

        /// <summary>
        /// Shared department faculty page. The sidebar Faculty link passes the department
        /// name encrypted in <paramref name="d"/>; the name is matched against
        /// dbo.tbl_Department to resolve the DepartmentId, then the teaching staff of that
        /// department are loaded from dbo.tbl_TeachingStaff.
        /// </summary>
        public ActionResult Dept_Faculty(string d)
        {
            string departmentName;
            if (!DeptDirectory.TryReadToken(d, out departmentName)) return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            var model = BuildDeptFacultyModel(departmentName);
            if (model == null) return HttpNotFound();
            return View(model);
        }

        /// <summary>
        /// Friendly department URLs: /department/{dept}/{page}. The slug picks the view that
        /// used to live at /Home/{action}, so the address bar stays readable while the page
        /// itself is unchanged.
        /// </summary>
        public ActionResult DepartmentPage(string dept, string page)
        {
            var action = DeptRoutes.ActionFor(dept, page);
            if (action == null) return HttpNotFound();
            if (action != DeptRoutes.FacultyAction) return View(action);

            var departmentName = DeptDirectory.NameOf(DeptRoutes.CodeFromSlug(dept));
            var model = BuildDeptFacultyModel(departmentName);
            if (model == null) return HttpNotFound();
            return View(DeptRoutes.FacultyAction, model);
        }

        private DeptFacultyViewModel BuildDeptFacultyModel(string departmentName)
        {
            if (string.IsNullOrWhiteSpace(departmentName)) return null;
            var sidebar = DeptDirectory.SidebarFor(departmentName);
            if (sidebar == null) return null;
            int departmentId;
            if (!TryGetDepartmentId(departmentName, out departmentId)) return null;
            return new DeptFacultyViewModel
            {
                DepartmentId = departmentId,
                DepartmentName = departmentName,
                SidebarPath = sidebar,
                Faculty = LoadPublicFaculty(null, departmentId)
            };
        }

        private static bool TryGetDepartmentId(string departmentName, out int departmentId)
        {
            departmentId = 0;
            using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["recpLocalDb"].ConnectionString))
            using (var command = new SqlCommand("SELECT TOP 1 DepartmentId FROM dbo.tbl_Department WHERE DepartmentName=@name AND IsActive=1", connection))
            {
                command.Parameters.Add("@name", System.Data.SqlDbType.NVarChar, 150).Value = departmentName;
                connection.Open();
                var result = command.ExecuteScalar();
                if (result == null || result == DBNull.Value) return false;
                departmentId = Convert.ToInt32(result);
                return true;
            }
        }

        private List<PublicFacultyViewModel> LoadPublicFaculty(int? staffId=null,int? departmentId=null)
        {
            var data=new List<PublicFacultyViewModel>();
            using(var connection=new SqlConnection(ConfigurationManager.ConnectionStrings["recpLocalDb"].ConnectionString))
            using(var command=new SqlCommand(@"SELECT s.StaffId,s.PhotoPath,s.FullName,s.Email,s.AlternateEmail,s.MobileNumber,s.LandlineNumber,
                s.Designation,d.DepartmentName,s.Qualification,s.LongDescription
                FROM dbo.tbl_TeachingStaff s INNER JOIN dbo.tbl_Department d ON d.DepartmentId=s.DepartmentId
                WHERE s.IsActive=1 AND (@id IS NULL OR s.StaffId=@id) AND (@deptId IS NULL OR s.DepartmentId=@deptId)
                ORDER BY CASE WHEN s.DisplayOrder IS NULL THEN 1 ELSE 0 END,s.DisplayOrder,s.StaffId DESC",connection))
            {command.Parameters.Add("@id",System.Data.SqlDbType.Int).Value=staffId.HasValue?(object)staffId.Value:DBNull.Value;command.Parameters.Add("@deptId",System.Data.SqlDbType.Int).Value=departmentId.HasValue?(object)departmentId.Value:DBNull.Value;connection.Open();using(var reader=command.ExecuteReader())while(reader.Read()){var id=reader.GetInt32(0);data.Add(new PublicFacultyViewModel{StaffId=id,ProfileToken=ProtectFacultyId(id),PhotoPath=reader.GetString(1),FullName=reader.GetString(2),Email=reader.GetString(3),AlternateEmail=reader.IsDBNull(4)?null:reader.GetString(4),MobileNumber=reader.IsDBNull(5)?null:reader.GetString(5),LandlineNumber=reader.IsDBNull(6)?null:reader.GetString(6),Designation=reader.GetString(7),DepartmentName=reader.GetString(8),Qualification=reader.GetString(9),LongDescription=reader.IsDBNull(10)?null:reader.GetString(10)});}}
            return data;
        }

        private static string ProtectFacultyId(int id){var bytes=System.Text.Encoding.UTF8.GetBytes(id.ToString(System.Globalization.CultureInfo.InvariantCulture));return HttpServerUtility.UrlTokenEncode(System.Web.Security.MachineKey.Protect(bytes,"FacultyProfile"));}
        private static bool TryUnprotectFacultyId(string token,out int id){id=0;if(string.IsNullOrWhiteSpace(token))return false;try{var encoded=HttpServerUtility.UrlTokenDecode(token);if(encoded==null)return false;var bytes=System.Web.Security.MachineKey.Unprotect(encoded,"FacultyProfile");return bytes!=null&&int.TryParse(System.Text.Encoding.UTF8.GetString(bytes),out id)&&id>0;}catch{return false;}}
        public ActionResult VisionM()
        {
            return View();
        }
        public ActionResult STRATEGIC_GROWTH_PLAN()
        {
            return View();
        }
        public ActionResult Dean_Academics()
        {
            return View();
        }

        public ActionResult Dean_Student_Welfare()
        {
            return View();
        }
        public ActionResult Notices()
        {
            return View();
        }


        public ActionResult Dr_Arvind_Profile()
        {
            return View();
        }
        public ActionResult Dr_Nitish_Jauhari()
        {
            return View();
        }
        public ActionResult Dr_Santosh_Pandey()
        {
            return View();
        }
        public ActionResult Er_Asit_Singh()
        {
            return View();
        }
        public ActionResult SandeepKumarVishwakarma()
        {
            return View();
        }
        public ActionResult DrRadhaVishwakarma()
        {
            return View();
        }

        public ActionResult Dr_arvind()
        {
            return View();
        }
        public ActionResult Er_Prashant() {

            return View();
        }
        public ActionResult Dinesh_Kumar() { 
        
            return View();
        }
        public ActionResult Er_Shivam_Mishra() 
        {
            return View();
        }
        public ActionResult Er_Shubham_Mishra() { 
        
        return View();
        }
        public ActionResult Ms_Utkarsha_Baish()
        {
            return View();
        }
        public ActionResult Alok_Sanyal() { 
        return View();
        }
        public ActionResult Dr_Piyush_Gupta()
        {

            return View();
        }
        public ActionResult Er_Pramesh_Kumar() {
            return View();       
        }
        public ActionResult Ms_Pallavi_Singh(){
            return View();
        }
        public ActionResult Er_Anand_Pratap_Singh()
        {
            return View();
        }
        public ActionResult ASH()
        {
            return View();
        }
        public ActionResult ASH_Vision()
        {
            return View();
        }
        public ActionResult ASH_Mision()
        {
            return View();
        }
        public ActionResult ASH_Faculty()
        {
            return View();
        }
        public ActionResult ASH_Facilities()
        {
            return View();
        }
        public ActionResult Engineering_Physics_Laboratory()
        {
            return View();
        }
        public ActionResult Engineering_Chemistry_Laboratory()
        {
            return View();
        }
        public ActionResult Language_and_Soft_Skills_Lab()
        {
            return View();
        }
        public ActionResult MCHE()
        {
            return View();
        }
        public ActionResult MCHE_Vision()
        {
            return View();
        }
        public ActionResult MCHE_Mision()
        {
            return View();
        }
        public ActionResult MCHE_Programme_offered()
        {
            return View();
        }
        public ActionResult MCHE_Faculty()
        {
            return View();
        }
        public ActionResult MCHE_Facilities()
        {
            return View();
        }
        public ActionResult Engineering_Graphics_Design_Lab()
        {
            return View();
        }
        public ActionResult Workshop_Practice_Lab()
        {
            return View();
        }
        public ActionResult Fluid_Mechanics_Lab()
        {
            return View();
        }
        public ActionResult ComputerAided_Design_Lab()
        {
            return View();
        }
        public ActionResult Material_Testing_Lab()
        {
            return View();
        }
        public ActionResult Applied_Thermodynamics_Lab()
        {
            return View();
        }
        public ActionResult Manufacturing_Processes_Lab()
        {
            return View();
        }

        public ActionResult CVLE()
        {
            return View();
        }
        public ActionResult CVLE_Vision()
        {
            return View();
        }
        public ActionResult CVLE_Mission()
        {
            return View();
        }
        public ActionResult CVLE_Faculty()
        {
            return View();
        }
        public ActionResult CVLE_Offer_Courses()
        {
            return View();
        }
        public ActionResult Engineering_Graphics_and_Design_Lab()
        {
            return View();
        }
        public ActionResult Building_Planning_and_Drawing_Lab()
        {
            return View();
        }
        public ActionResult Surveying_and_Geomatics_Lab()
        {
            return View();
        }
        public ActionResult Fluid_Mechanics_Labs()
        {
            return View();
        }
        public ActionResult CAD_Lab()
        {
            return View();
        }
        public ActionResult Quantity_Estimation_and_Management_Lab()
        {
            return View();
        }
        public ActionResult CVLE_Facilities()
        {
            return View();
        }
        public ActionResult ELE()
        {
            return View();
        }
        public ActionResult ELE_Vision()
        {
            return View();
        }
        public ActionResult ELE_Mission()
        {
            return View();
        }
        public ActionResult ELE_Programmes_offered()
        {
            return View();
        }
        public ActionResult ELE_Facilities()
        {
            return View();
        }
        public ActionResult ELE_Faculty()
        {
            return View();
        }
        public ActionResult Basic_Electrical_Engineering_Lab()
        {
            return View();
        }
        public ActionResult Electrical_Workshop_Lab()
        {
            return View();
        }
        public ActionResult Electrical_Machine_Lab()
        {
            return View();
        }
        public ActionResult Electrical_Measurement_Instrumentation_Lab()
        {
            return View();
        }
        public ActionResult Fundamental_Electronics_Engineering_Lab()
        {
            return View();
        }
        public ActionResult Circuit_Simulation_Lab()
        {
            return View();
        }

        public ActionResult CS()
        {
            return View();
        }
        public ActionResult CS_Vision()
        {
            return View();
        }
        public ActionResult CS_Mission()
        {
            return View();
        }
        public ActionResult CS_Programmes_offered()
        {
            return View();
        }
        public ActionResult CS_Faculty()
        {
            return View();
        }
        public ActionResult Programming_Lab()
        {
            return View();
        }
        public ActionResult Data_Structures_Algorithms_Lab()
        {
            return View();
        }

        public ActionResult Computer_Networks_Lab()
        {
            return View();
        }
        public ActionResult Operating_Systems_Lab()
        {
            return View();
        }
        public ActionResult Database_Management_Systems_Lab()
        {
            return View();
        }
        public ActionResult Artificial_Intelligence_Machine_Learning_Lab()
        {
            return View();
        }
        public ActionResult Web_Technology_Lab()
        {
            return View();
        }
        public ActionResult CENTRAL_LIBRARY()
        {
            return View();
        }
        public ActionResult TRAINING_PLACEMENT_CELL()
        {
            return View();
        }
        public ActionResult IIC()
        {
            return View();
        }

        public ActionResult Demo()
        {
            return View();
        }
        public ActionResult ARPIT_SAMANT()
        {
            return View();
        }
        public ActionResult Shubham_Mishra()
        {
            return View();
        }
        public ActionResult testpage()
        {
            return View();
        }
    }

}
