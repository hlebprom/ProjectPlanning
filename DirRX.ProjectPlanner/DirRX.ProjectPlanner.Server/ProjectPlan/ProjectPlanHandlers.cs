using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanner.ProjectPlan;

namespace DirRX.ProjectPlanner
{
	partial class ProjectPlanDocumentKindPropertyFilteringServerHandler<T>
	{

		public virtual IQueryable<T> DocumentKindFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
		{
			return query.Where(x => x.DocumentType.DocumentTypeGuid == Server.ProjectPlan.ClassTypeGuid.ToString());
		}
	}

	partial class ProjectPlanFilteringServerHandler<T>
	{

		public override IQueryable<T> Filtering(IQueryable<T> query, Sungero.Domain.FilteringEventArgs e)
		{
			if (_filter == null)
				return query;
			
			// Фильтр по состоянию.
			if (_filter.Active || _filter.Closed || _filter.Closing || _filter.Initiation)
				query = query.Where(x => (_filter.Active && x.Stage == Stage.Execution) ||
				                    (_filter.Closed && x.Stage == Stage.Completed) ||
				                    (_filter.Closing && x.Stage == Stage.Completion) ||
				                    (_filter.Initiation && x.Stage == Stage.Initiation));

			var today = Calendar.UserToday;
			
			// Фильтр по дате начала проекта.
			var startDateBeginPeriod = _filter.StartDateRangeFrom ?? Calendar.SqlMinValue;
			var startDateEndPeriod = _filter.StartDateRangeTo ?? Calendar.SqlMaxValue;
			
			if (_filter.StartPeriodThisMonth)
			{
				startDateBeginPeriod = today.BeginningOfMonth();
				startDateEndPeriod = today.EndOfMonth();
			}
			
			if (_filter.StartPeriodThisMonth || (_filter.StartDateRangeFrom != null || _filter.StartDateRangeTo != null))
				query = query.Where(x => (x.StartDate.Between(startDateBeginPeriod, startDateEndPeriod) && !Equals(x.Stage, Stage.Completed)) ||
				                    (x.ActualStartDate.Between(startDateBeginPeriod, startDateEndPeriod) && Equals(x.Stage, Stage.Completed)));

			// Фильтр по дате окончания проекта.
			var finishDateBeginPeriod = _filter.FinishDateRangeFrom ?? Calendar.SqlMinValue;
			var finishDateEndPeriod = _filter.FinishDateRangeTo ?? Calendar.SqlMaxValue;
			
			if (_filter.FinishPeriodThisMonth)
			{
				finishDateBeginPeriod = today.BeginningOfMonth();
				finishDateEndPeriod = today.EndOfMonth();
			}
			
			if (_filter.FinishPeriodThisMonth || (_filter.FinishDateRangeFrom != null || _filter.FinishDateRangeTo != null))
				query = query.Where(x => (x.EndDate.Between(finishDateBeginPeriod, finishDateEndPeriod) && !Equals(x.Stage, Stage.Completed)) ||
				                    (x.ActualFinishDate.Between(finishDateBeginPeriod, finishDateEndPeriod) && Equals(x.Stage, Stage.Completed)));
			
			return query;
		}
	}

	partial class ProjectPlanCreatingFromServerHandler
	{

		public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
		{
			e.Without(_info.Properties.ActualStartDate);
			e.Without(_info.Properties.ActualFinishDate);
			e.Without(_info.Properties.ExecutionPercent);
			e.Without(_info.Properties.Note);
			e.Params.Add("IsCopy", true);
		}
	}

	partial class ProjectPlanServerHandlers
	{

		public override void BeforeSaveHistory(Sungero.Content.DocumentHistoryEventArgs e)
		{
			var action = new Enumeration("Manage");
			if (e.Action == action)
			{
				var prAct = DirRX.ProjectPlanner.ProjectActivities.GetAll(x => ProjectPlans.Equals(x.ProjectPlan, _obj));
				foreach (var act in prAct)
				{
					Logger.DebugFormat("Актвивити ИД: {0}", act.Id);
					foreach (var accRightsProject in _obj.AccessRights.Current)
					{
						foreach (var accRightsActivity in act.AccessRights.Current)
						{
							act.AccessRights.RevokeAll(accRightsActivity.Recipient);
							act.Save();
						}
						act.AccessRights.Grant(accRightsProject.Recipient, accRightsProject.AccessRightsType);
						act.Save();
					}
				}
			}
		}

		public override void Created(Sungero.Domain.CreatedEventArgs e)
		{
			_obj.Stage = Stage.Initiation;
			
			_obj.Modified = Calendar.Now;
			
			_obj.BaselineWorkType = ProjectPlan.BaselineWorkType.Hours;
			
			_obj.StartDate = Calendar.Now;
			
			_obj.DocumentKind = Sungero.Docflow.DocumentKinds.GetAll(x => x.DocumentType.DocumentTypeGuid == Server.ProjectPlan.ClassTypeGuid.ToString()).First();
			
			var isCopy = false;
			e.Params.TryGetValue("IsCopy", out isCopy);
			_obj.IsCopy = isCopy;
		}

		public override void Deleting(Sungero.Domain.DeletingEventArgs e)
		{
			var linkedProject = DirRX.ProjectPlanning.Projects.GetAll(x => ProjectPlans.Equals(_obj, x.ProjectPlanDirRX)).FirstOrDefault();
			
			var recipient = linkedProject != null ? linkedProject.Administrator : _obj.Author;
			// Изъять права на этапы проекта.
			ProjectPlanner.ProjectActivities.AccessRights.RevokeAll(recipient);
			ProjectPlanner.ProjectActivities.AccessRights.Save();
		}

		public override void AfterSave(Sungero.Domain.AfterSaveEventArgs e)
		{
			if (!e.Params.Contains(Constants.Module.DontUpdateModified) && e.Params.Contains(Sungero.Docflow.PublicConstants.OfficialDocument.GrantAccessRightsToProjectDocument))
			{
				Sungero.Projects.Jobs.GrantAccessRightsToProjectDocuments.Enqueue();
				e.Params.Remove(Sungero.Docflow.PublicConstants.OfficialDocument.GrantAccessRightsToProjectDocument);
			}
			
			if (!e.Params.Contains(Constants.Module.DontUpdateModified))
				Sungero.Projects.Jobs.GrantAccessRightsToProjectFolders.Enqueue();
			
			
			var linkedProject = DirRX.ProjectPlanning.Projects.GetAll(x => ProjectPlans.Equals(_obj, x.ProjectPlanDirRX)).FirstOrDefault();
			
			// Выдать права администратору проекта на создание/изменение этапов проекта.
			var recipient = linkedProject != null ? linkedProject.Administrator : _obj.Author;
			
			if (recipient != null)
			{
				ProjectPlanner.ProjectActivities.AccessRights.Grant(recipient, DefaultAccessRightsTypes.FullAccess);
				ProjectPlanner.ProjectActivities.AccessRights.Save();
			}
			
			if (linkedProject != null)
			{
				if (!linkedProject.Folder.Items.Contains(_obj))
					linkedProject.Folder.Items.Add(_obj);
			}
			
			if (CallContext.CalledFrom(DirRX.ProjectPlanning.Projects.Info))
			{
				var project = DirRX.ProjectPlanning.Projects.Get(CallContext.GetCallerEntityId(DirRX.ProjectPlanning.Projects.Info));
				
				if (!ProjectPlans.Equals(project.ProjectPlanDirRX, _obj))
					project.ProjectPlanDirRX = _obj;
			}

		}

		public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
		{
			if (!_obj.AccessRights.CanUpdate())
			{
				e.AddError(ProjectPlans.Resources.NoRightToUpdateProject);
				return;
			}
			
			// TODO Zamerov: сравнивать надо с ресурсом в локали тенанта. BUG: 35010
			if (Equals(_obj.Name, Sungero.Projects.Resources.ProjectArhiveFolderName))
				e.AddError(ProjectPlans.Resources.PropertyReservedFormat(_obj.Info.Properties.Name.LocalizedName, Sungero.Projects.Resources.ProjectArhiveFolderName));
			
			if (ProjectPlans.GetAll().Any(p => !Equals(p, _obj) && Equals(p.Name, _obj.Name)))
				e.AddError(ProjectPlans.Resources.PropertyAlreadyUsedFormat(_obj.Info.Properties.Name.LocalizedName, _obj.Name));

			if (!e.Params.Contains(Constants.Module.DontUpdateModified))
				_obj.Modified = Calendar.Now;
			
		}
	}
	partial class ProjectPlanTeamMembersMemberPropertyFilteringServerHandler<T>
	{

		public virtual IQueryable<T> TeamMembersMemberFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
		{
			return query.Where(x => x.Sid != Sungero.Domain.Shared.SystemRoleSid.Administrators &&
			                   x.Sid != Sungero.Domain.Shared.SystemRoleSid.Auditors &&
			                   x.Sid != Sungero.Domain.Shared.SystemRoleSid.ConfigurationManagers &&
			                   x.Sid != Sungero.Domain.Shared.SystemRoleSid.ServiceUsers &&
			                   x.Sid != Sungero.Domain.Shared.SystemRoleSid.SoloUsers &&
			                   x.Sid != Sungero.Domain.Shared.SystemRoleSid.DeliveryUsersSid);
		}
	}

}