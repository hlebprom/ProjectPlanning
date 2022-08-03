using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanner.ProjectPlanRX;

namespace DirRX.ProjectPlanner
{
  partial class ProjectPlanRXDocumentKindPropertyFilteringServerHandler<T>
  {

    public override IQueryable<T> DocumentKindFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query.Where(x => x.DocumentType.DocumentTypeGuid == Server.ProjectPlanRX.ClassTypeGuid.ToString());
    }
  }

  partial class ProjectPlanRXFilteringServerHandler<T>
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

  partial class ProjectPlanRXCreatingFromServerHandler
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

  partial class ProjectPlanRXServerHandlers
  {

    public override void BeforeSaveHistory(Sungero.Content.DocumentHistoryEventArgs e)
    {
      base.BeforeSaveHistory(e);
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      _obj.Stage = Stage.Initiation;
      
      _obj.Modified = Calendar.Now;
      
      _obj.BaselineWorkType = ProjectPlanRX.BaselineWorkType.Hours;
      
      _obj.StartDate = Calendar.Now;
      
      var defaultOrFirstActiveDocKind = Sungero.Docflow.DocumentKinds
        .GetAll(x => x.DocumentType.DocumentTypeGuid == Server.ProjectPlanRX.ClassTypeGuid.ToString() &&
          x.Status == Sungero.Docflow.DocumentKind.Status.Active)
        .OrderByDescending(x => x.IsDefault.HasValue && x.IsDefault.Value)
        .FirstOrDefault();
      
      if (defaultOrFirstActiveDocKind == null)
      {
        throw new ArgumentException(DirRX.ProjectPlanner.ProjectPlanRXes.Resources.CannotFindDocKind);
      }
      
      _obj.DocumentKind = defaultOrFirstActiveDocKind;
      
      var isCopy = false;
      e.Params.TryGetValue("IsCopy", out isCopy);
      _obj.IsCopy = isCopy;
    }

    public override void Deleting(Sungero.Domain.DeletingEventArgs e)
    {
      var linkedProject = DirRX.ProjectPlanning.Projects.GetAll(x => ProjectPlanRXes.Equals(_obj, x.ProjectPlanDirRX)).FirstOrDefault();
      
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
      
      
      var linkedProject = DirRX.ProjectPlanning.Projects.GetAll(x => ProjectPlanRXes.Equals(_obj, x.ProjectPlanDirRX)).FirstOrDefault();
      
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
      
      if(_obj.ProjectId.HasValue)
      {
        var project = DirRX.ProjectPlanning.Projects.Get(_obj.ProjectId.Value);
        if (project != null)
        {
          if (!ProjectPlanRXes.Equals(project.ProjectPlanDirRX, _obj))
            project.ProjectPlanDirRX = _obj;
        }
      }

    }
    
    private void UpdateLinkedProject(DirRX.ProjectPlanning.IProject linkedProject)
    {
      if (linkedProject == null)
      {
        return;
      }
      
      if (!linkedProject.AccessRights.CanUpdate())
      {
        Logger.Error(DirRX.ProjectPlanner.ProjectPlanRXes.Resources.NotEnoughRightsToUpdateProjectFormat(linkedProject.Name));
        return;
      }
      
      if (Locks.GetLockInfo(linkedProject).IsLockedByOther)
      {
        Logger.Error(DirRX.ProjectPlanner.ProjectPlanRXes.Resources.ProjectIsLockedByOtherFormat(linkedProject.Name));
        return;
      }
      
      if(_obj.State.Properties.StartDate.IsChanged && _obj.StartDate != null)
      {
        linkedProject.StartDate = _obj.StartDate;
      }
      
      if(_obj.State.Properties.EndDate.IsChanged && _obj.EndDate != null)
      {
        linkedProject.EndDate = _obj.EndDate;
      }
      
      if(_obj.State.Properties.ActualStartDate.IsChanged && _obj.ActualStartDate != null)
      {
        linkedProject.ActualStartDate = _obj.ActualStartDate;
      }
      
      if(_obj.State.Properties.ActualFinishDate.IsChanged && _obj.ActualFinishDate != null)
      {
        linkedProject.ActualFinishDate = _obj.ActualFinishDate;
      }
    }

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      if (!_obj.AccessRights.CanUpdate())
      {
        e.AddError(ProjectPlanRXes.Resources.NoRightToUpdateProject);
        return;
      }
      
      //TODO Urmanov Нагруженная часть, можно отказаться при отказе от связи с проектом
      var linkedProject = Functions.ProjectPlanRX.GetLinkedProject(_obj);
      
      UpdateLinkedProject(linkedProject);
      
      
      // TODO Zamerov: сравнивать надо с ресурсом в локали тенанта. BUG: 35010
      if (Equals(_obj.Name, Sungero.Projects.Resources.ProjectArhiveFolderName))
        e.AddError(ProjectPlanRXes.Resources.PropertyReservedFormat(_obj.Info.Properties.Name.LocalizedName, Sungero.Projects.Resources.ProjectArhiveFolderName));

      if (!e.Params.Contains(Constants.Module.DontUpdateModified))
        _obj.Modified = Calendar.Now; 
      
    }
  }
  partial class ProjectPlanRXTeamMembersMemberPropertyFilteringServerHandler<T>
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