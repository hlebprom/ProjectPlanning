using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanner.ProjectActivity;

namespace DirRX.ProjectPlanner
{

  partial class ProjectActivityServerHandlers
  {

    public override void BeforeDelete(Sungero.Domain.BeforeDeleteEventArgs e)
    {
    }

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      if(_obj.Responsible != null && _obj.State.Properties.Responsible.IsChanged)
      {
        _obj.AccessRights.Grant(_obj.Responsible, DefaultAccessRightsTypes.Change);
      }
    }

    public override void AfterSave(Sungero.Domain.AfterSaveEventArgs e)
    {
      if(_obj.Responsible != null)
      {
        var project = ProjectPlanning.Projects.GetAll(x => ProjectPlanRXes.Equals(x.ProjectPlanDirRX, _obj.ProjectPlan)).FirstOrDefault();
                
        var asyncHandler = ProjectPlanner.AsyncHandlers.GrantRightsActivityResponsible.Create();
        asyncHandler.EmployeeId = _obj.Responsible.Id;
        asyncHandler.ProjectPlanId = _obj.ProjectPlan.Id;
        asyncHandler.ProjectId = project != null ? project.Id : 0;
        asyncHandler.ExecuteAsync();
      }
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      //Для того, что бы проинициализировать св-во, но не смешивать с другими активити.
      _obj.NumberVersion = -10;
    }
  }

}