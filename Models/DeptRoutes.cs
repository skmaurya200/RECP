using System;
using System.Collections.Generic;
using System.Linq;

namespace Rec_Partapgarh.Models
{
    /// <summary>
    /// Friendly URLs for the department section: /department/{dept}/{page}.
    /// Each department slug owns its own page-slug -> Home view/action table, so labs that
    /// exist twice under different names (e.g. Fluid Mechanics Lab in Civil and Mechanical)
    /// resolve to the right view. The old /Home/{action} URLs keep working unchanged.
    /// </summary>
    public static class DeptRoutes
    {
        public const string FacultyAction = "Dept_Faculty";

        private class Dept
        {
            public string Code;
            public string Slug;
            public Dictionary<string, string> Pages;
        }

        private static readonly Dept[] All =
        {
            new Dept
            {
                Code = "CS", Slug = "computer-science-and-engineering",
                Pages = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "about", "CS" },
                    { "vision-mission", "CS_Vision" },
                    { "programme-offered", "CS_Programmes_offered" },
                    { "faculty", FacultyAction },
                    { "programming-lab", "Programming_Lab" },
                    { "data-structures-algorithms-lab", "Data_Structures_Algorithms_Lab" },
                    { "computer-networks-lab", "Computer_Networks_Lab" },
                    { "operating-systems-lab", "Operating_Systems_Lab" },
                    { "web-technology-lab", "Web_Technology_Lab" },
                    { "database-management-systems-lab", "Database_Management_Systems_Lab" },
                    { "artificial-intelligence-machine-learning-lab", "Artificial_Intelligence_Machine_Learning_Lab" }
                }
            },
            new Dept
            {
                Code = "ELE", Slug = "electrical-engineering",
                Pages = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "about", "ELE" },
                    { "vision-mission", "ELE_Vision" },
                    { "programme-offered", "ELE_Programmes_offered" },
                    { "faculty", FacultyAction },
                    { "basic-electrical-engineering-lab", "Basic_Electrical_Engineering_Lab" },
                    { "electrical-workshop-lab", "Electrical_Workshop_Lab" },
                    { "electrical-machine-lab", "Electrical_Machine_Lab" },
                    { "electrical-measurement-instrumentation-lab", "Electrical_Measurement_Instrumentation_Lab" },
                    { "fundamental-electronics-engineering-lab", "Fundamental_Electronics_Engineering_Lab" },
                    { "circuit-simulation-lab", "Circuit_Simulation_Lab" }
                }
            },
            new Dept
            {
                Code = "CVLE", Slug = "civil-engineering",
                Pages = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "about", "CVLE" },
                    { "vision-mission", "CVLE_Vision" },
                    { "programme-offered", "CVLE_Offer_Courses" },
                    { "faculty", FacultyAction },
                    { "engineering-graphics-and-design-lab", "Engineering_Graphics_and_Design_Lab" },
                    { "building-planning-and-drawing-lab", "Building_Planning_and_Drawing_Lab" },
                    { "surveying-and-geomatics-lab", "Surveying_and_Geomatics_Lab" },
                    { "fluid-mechanics-lab", "Fluid_Mechanics_Labs" },
                    { "cad-lab", "CAD_Lab" },
                    { "quantity-estimation-and-management-lab", "Quantity_Estimation_and_Management_Lab" }
                }
            },
            new Dept
            {
                Code = "MCHE", Slug = "mechanical-engineering",
                Pages = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "about", "MCHE" },
                    { "vision-mission", "MCHE_Vision" },
                    { "programme-offered", "MCHE_Programme_offered" },
                    { "faculty", FacultyAction },
                    { "engineering-graphics-design-lab", "Engineering_Graphics_Design_Lab" },
                    { "workshop-practice-lab", "Workshop_Practice_Lab" },
                    { "fluid-mechanics-lab", "Fluid_Mechanics_Lab" },
                    { "computer-aided-design-lab", "ComputerAided_Design_Lab" },
                    { "material-testing-lab", "Material_Testing_Lab" },
                    { "applied-thermodynamics-lab", "Applied_Thermodynamics_Lab" },
                    { "manufacturing-processes-lab", "Manufacturing_Processes_Lab" }
                }
            },
            new Dept
            {
                Code = "ASH", Slug = "applied-sciences-and-humanities",
                Pages = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "about", "ASH" },
                    { "vision-mission", "ASH_Vision" },
                    { "faculty", FacultyAction },
                    { "engineering-physics-laboratory", "Engineering_Physics_Laboratory" },
                    { "engineering-chemistry-laboratory", "Engineering_Chemistry_Laboratory" },
                    { "language-and-soft-skills-lab", "Language_and_Soft_Skills_Lab" }
                }
            }
        };

        private static Dept BySlug(string slug)
        {
            return All.FirstOrDefault(x => string.Equals(x.Slug, slug, StringComparison.OrdinalIgnoreCase));
        }

        public static string CodeFromSlug(string slug)
        {
            var dept = BySlug(slug);
            return dept == null ? null : dept.Code;
        }

        /// <summary>Home view/action name for a friendly URL, or null when the URL is unknown.</summary>
        public static string ActionFor(string slug, string page)
        {
            var dept = BySlug(slug);
            if (dept == null) return null;
            string action;
            return dept.Pages.TryGetValue((page ?? "about").Trim(), out action) ? action : null;
        }
    }
}
