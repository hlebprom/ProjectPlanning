using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Initialization;

namespace DirRX.ProjectPlanner.Server
{
	public partial class ModuleInitializer
	{

		public override void Initializing(Sungero.Domain.ModuleInitializingEventArgs e)
		{
			// Назначить права роли "Руководители проектов".
			Logger.Debug("Init: Grant rights on project activity");
			GrantRightsOnProjectActivity();
			GrantRightsOnProjectPlans();
			CreateAssociatedApplication();
			CreateDocumentType();
			CreateDocumentKinds();
			GrantRightsOnOpensProjectPlansFromCards();
			GrantRightsOnProjectActivityToProjectManager();
		}
		
		public static void GrantRightsOnOpensProjectPlansFromCards()
		{
			ProjectPlanner.OpensProjectPlansFromCards.AccessRights.Grant(Roles.AllUsers, DefaultAccessRightsTypes.FullAccess);
			ProjectPlanner.OpensProjectPlansFromCards.AccessRights.Save();
		}
		
		public static void GrantRightsOnProjectPlans()
		{
			ProjectPlanner.ProjectPlans.AccessRights.Grant(Roles.AllUsers, DefaultAccessRightsTypes.Create);
			ProjectPlanner.ProjectPlans.AccessRights.Save();
		}
		
		public static void GrantRightsOnProjectActivity()
		{
			ProjectPlanner.ProjectActivities.AccessRights.Grant(Roles.AllUsers, DefaultAccessRightsTypes.Create);
			ProjectPlanner.ProjectActivities.AccessRights.Save();
		}
		
		public static void GrantRightsOnProjectActivityToProjectManager()
		{
			var role = Sungero.Docflow.PublicInitializationFunctions.Module.GetProjectManagersRole();
			ProjectPlanner.ProjectActivities.AccessRights.Grant(role, DefaultAccessRightsTypes.FullAccess);
			ProjectPlanner.ProjectActivities.AccessRights.Save();
		}
		
		public static void CreateDocumentType()
		{
			InitializationLogger.Debug("Init: Create document type");
			
			Sungero.Docflow.PublicInitializationFunctions.Module.CreateDocumentType(DirRX.ProjectPlanner.Resources.ProjectPlanName, ProjectPlan.ClassTypeGuid, Sungero.Docflow.DocumentType.DocumentFlow.Inner, true);
		}
		
		public static void CreateDocumentKinds()
		{
			InitializationLogger.Debug("Init: Create document kinds.");
			
			var notNumerable = Sungero.Docflow.DocumentKind.NumberingType.NotNumerable;
			Sungero.Docflow.PublicInitializationFunctions.Module.CreateDocumentKind(DirRX.ProjectPlanner.Resources.ProjectPlanName,
			                                                                        DirRX.ProjectPlanner.Resources.ProjectPlanName, notNumerable,
			                                                                        Sungero.Docflow.DocumentKind.DocumentFlow.Inner, true, false, ProjectPlan.ClassTypeGuid, null,
			                                                                        Constants.Module.ProjectPlanDocKindGuid, true);
		}
		
		public static void CreateAssociatedApplication()
		{
			if (!Sungero.Content.AssociatedApplications.GetAll(x => x.Extension == "rxpp").Any())
			{
				var app = Sungero.Content.AssociatedApplications.Create();
				app.Extension = "rxpp";
				app.Name = DirRX.ProjectPlanner.Resources.AplicationName;
				app.MonitoringType = Sungero.Content.AssociatedApplication.MonitoringType.ByProcessAndWindow;
				var fileType = Sungero.Content.FilesTypes.Create();
				fileType.Name = DirRX.ProjectPlanner.Resources.ProjectsFileTypeName;
				app.FilesType = fileType;
				app.Save();
			}
		}
	}
}
