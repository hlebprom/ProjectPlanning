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

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      if(_obj.State.Properties.Stage.IsChanged && _obj.ProjectPlanDirRX != null)
      {
        var lockInfo = Locks.GetLockInfo(_obj.ProjectPlanDirRX);
        if (lockInfo.IsLockedByOther)
        {
          e.AddError(string.Format(DirRX.ProjectPlanning.Projects.Resources.CloseProjectErrorT, lockInfo.OwnerName));
          return;
        }
        
        if (_obj.Stage == Stage.Completed)
          _obj.ProjectPlanDirRX.Stage = ProjectPlanner.ProjectPlanRX.Stage.Completed;
        else if (_obj.Stage == Stage.Execution)
          _obj.ProjectPlanDirRX.Stage = ProjectPlanner.ProjectPlanRX.Stage.Execution;       

      } 
      base.BeforeSave(e);
    }

    public override void Saving(Sungero.Domain.SavingEventArgs e)
    {
      base.Saving(e);
      
      if (_obj.Administrator != null)
        ProjectPlanner.ProjectActivities.AccessRights.Grant(_obj.Administrator, DefaultAccessRightsTypes.FullAccess);
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