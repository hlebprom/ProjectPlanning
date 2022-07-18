using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanning.Project;

namespace DirRX.ProjectPlanning
{
	partial class ProjectServerHandlers
	{

		public override void Saving(Sungero.Domain.SavingEventArgs e)
		{
			base.Saving(e);
			
			if (_obj.Administrator != null)
				ProjectPlanner.ProjectActivities.AccessRights.Grant(_obj.Administrator, DefaultAccessRightsTypes.FullAccess);
		}

		public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
		{
			base.BeforeSave(e);
		}

		public override void AfterSave(Sungero.Domain.AfterSaveEventArgs e)
		{
			base.AfterSave(e);
			if (_obj.ProjectPlanDirRX != null && !_obj.Folder.Items.Contains(_obj.ProjectPlanDirRX))
				_obj.Folder.Items.Add(_obj.ProjectPlanDirRX);
		}

		public override void BeforeSaveHistory(Sungero.Domain.HistoryEventArgs e)
		{
			base.BeforeSaveHistory(e);
		}

		public override void Deleting(Sungero.Domain.DeletingEventArgs e)
		{
			// Изъять права на этапы проекта.
			ProjectPlanner.ProjectActivities.AccessRights.RevokeAll(_obj.Administrator);
			ProjectPlanner.ProjectActivities.AccessRights.Save();
			
			base.Deleting(e);
		}

		public override void Created(Sungero.Domain.CreatedEventArgs e)
		{
			base.Created(e);
			_obj.BaselineWorkType = Project.BaselineWorkType.Hours;
			_obj.StartDate = Calendar.Now;
		}
	}

}