using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanning.Project;

namespace DirRX.ProjectPlanning
{
  partial class ProjectClientHandlers
  {

    public override void Closing(Sungero.Presentation.FormClosingEventArgs e)
    {
      /*if (_obj.ProjectPlanDirRX != null && _obj.AccessRights.CanUpdate())
      {
        DirRX.ProjectPlanner.PublicFunctions.OpensProjectPlansFromCard.Remote.DeleteEntry(_obj);
        var lockInfo = Locks.GetLockInfo(_obj);
        if (lockInfo.IsLockedByMe)
          Locks.Unlock(_obj.ProjectPlanDirRX);
      }*/
      base.Closing(e);
    }

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {     
      Functions.Project.SetPropertiesAvailability(_obj);
      /*if(!CallContext.CalledFrom(ProjectPlanner.ProjectPlans.Info))
      {
        if (!_obj.State.IsInserted && !Locks.GetLockInfo(_obj).IsLockedByOther && _obj.AccessRights.CanUpdate())
        {
          if (_obj.ProjectPlanDirRX != null)
          {
            DirRX.ProjectPlanner.PublicFunctions.OpensProjectPlansFromCard.Remote.CreateEntry(_obj);
            Locks.TryLock(_obj.ProjectPlanDirRX);
          }
        }
      }*/
      base.Showing(e);
    }

    public override void EndDateValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      base.EndDateValueInput(e);
      if (_obj.ProjectPlanDirRX != null && ProjectPlanner.PublicFunctions.ProjectActivity.Remote.GetActivities(_obj.ProjectPlanDirRX).Any())
        e.AddError(Projects.Resources.DisableProperty);
    }

    public override void StartDateValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      base.StartDateValueInput(e);
      
      if (_obj.ProjectPlanDirRX != null && ProjectPlanner.PublicFunctions.ProjectActivity.Remote.GetActivities(_obj.ProjectPlanDirRX).Any())
        e.AddError(Projects.Resources.DisableProperty);
    }

    public override void ExecutionPercentValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
    {
      base.ExecutionPercentValueInput(e);
      
      if (_obj.ProjectPlanDirRX != null && ProjectPlanner.PublicFunctions.ProjectActivity.Remote.GetActivities(_obj.ProjectPlanDirRX).Any())
        e.AddError(Projects.Resources.DisableProperty);
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      base.Refresh(e);
      
      if (_obj.Stage != Stage.Completed)
        Functions.Project.SetPropertiesAvailability(_obj);
      

    }

    public virtual void BaselineWorkValueInput(Sungero.Presentation.DoubleValueInputEventArgs e)
    {
      if ((e.NewValue != null) && (e.NewValue.Value < 0))
        e.AddError(Projects.Resources.IncorrectBaselineWork);
      if (_obj.ProjectPlanDirRX != null && ProjectPlanner.PublicFunctions.ProjectActivity.Remote.GetActivities(_obj.ProjectPlanDirRX).Any())
        e.AddError(Projects.Resources.DisableProperty);
    }

  }
}