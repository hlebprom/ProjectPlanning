using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Workflow;
using DirRX.ProjectPlanner.ProjectActivityTask;
using Sungero.Domain.Clients;

namespace DirRX.ProjectPlanner.Server
{
  partial class ProjectActivityTaskRouteHandlers
  {

    public virtual bool Decision21Result()
    {
      return _obj.Subtasks.FirstOrDefault().Status == Status.Completed;
    }

    public virtual void StartReviewAssignment2(Sungero.Workflow.IReviewAssignment reviewAssignment)
    {
    }

    public virtual void StartBlock10(DirRX.ProjectPlanner.Server.RXTaskNoticeArguments e)
    {
      foreach (var responsible in _obj.Responsibles)
      {
        e.Block.Performers.Add(responsible.Responsible);
      }
      e.Block.Subject = DirRX.ProjectPlanner.ProjectActivityTasks.Resources.FinishedWorksOnStageFormat(_obj.ProjectActivity.Name);
      e.Block.ProjectPlan = _obj.ProjectPlan;
      e.Block.ProjectActivity = _obj.ProjectActivity;
    }

    public virtual void StartBlock11(DirRX.ProjectPlanner.Server.AssignmentArguments e)
    {
      foreach (var responsible in _obj.Responsibles)
      {
        e.Block.Performers.Add(responsible.Responsible);
      }
      
      e.Block.AbsoluteDeadline = _obj.MaxDeadline.Value;
      e.Block.Subject = _obj.Subject;
      e.Block.ProjectActivity = _obj.ProjectActivity;
      e.Block.ProjectPlan = _obj.ProjectPlan;
    }

    public virtual void Script9Execute()
    {
      Functions.ProjectActivityTask.SendTaskStarted(_obj);
    }

    public virtual void Script5Execute()
    {
      Functions.ProjectActivityTask.SendTaskEnded(_obj);
    }

  }
}