using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.ProjectPlanner.Server
{
	public class ModuleJobs
	{

		public virtual void SyncAccessRightsProjectPlanAndProject()
		{
			var projects = DirRX.ProjectPlanning.Projects.GetAll(x => x.ProjectPlanDirRX != null);
			

			foreach (var project in projects)
			{
				var oldRecs = project.ProjectPlanDirRX.AccessRights.Current.Select(x => x.Recipient);
				
				var projectPlan = project.ProjectPlanDirRX;
				
				foreach (var oldRec in oldRecs)
					projectPlan.AccessRights.RevokeAll(oldRec);
				
				foreach (var recipent in project.AccessRights.Current.Select(x => x.Recipient))
				{
					projectPlan.AccessRights.Grant(recipent, project.AccessRights.Current.Where(x => Recipients.Equals(x.Recipient, recipent)).Select(x => x.AccessRightsType).ToArray());
				}
				
				
				try
				{
					projectPlan.Save();
				}
				catch (Exception ex)
				{
					Logger.DebugFormat("Произошли ошибка в ФП SyncAccessRightsProjectPlanAndProject при обработке проекта {0} - {1}: {2}", project.Id, project.DisplayValue, ex.Message);
				}
			}
			
		}

	}
}