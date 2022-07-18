using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.ProjectPlanner.Server
{
  partial class IncomeProjectsExecutionFolderHandlers
  {

    public virtual bool IsIncomeProjectsExecutionVisible()
    {
      return true;
    }

    public virtual IQueryable<DirRX.ProjectPlanner.IAssignment> IncomeProjectsExecutionDataQuery(IQueryable<DirRX.ProjectPlanner.IAssignment> query)
    {
      if (_filter == null)
      {
        return query;
      }
            
      if (_filter.Me)
      {
        query = query.Where(t => t.Performer.Id == Users.Current.Id);
      }
      
      if (_filter.Selected && _filter.PerformerSelector != null)
      {
        query = query.Where(t => t.Performer.Id == _filter.PerformerSelector.Id);
      }
      
      if (_filter.ProjectPlanSelector != null)
      {
        query = query.Where(t => ProjectActivityTasks.As(t.MainTask).ProjectPlan.Id == _filter.PerformerSelector.Id);
      }
      
      return query;
    }
  }

  partial class ProjectsExecutionFolderHandlers
  {

    public virtual IQueryable<DirRX.ProjectPlanner.IProjectActivityTask> ProjectsExecutionDataQuery(IQueryable<DirRX.ProjectPlanner.IProjectActivityTask> query)
    {
      if (_filter == null)
      {
        return query;
      }
      
      if (_filter.Me)
      {
        query = query.Where(t => t.Responsibles.Select(r => r.Id).Contains(Users.Current.Id));
      }
      
      if (_filter.Selected && _filter.AuthorSelector != null)
      {
        query = query.Where(t => t.Responsibles.Select(r => r.Id).Contains(_filter.AuthorSelector.Id));
      }
      
      return query;
    }

    public virtual bool IsProjectsExecutionVisible()
    {
      return true;
    }
  }


  partial class ProjectPlannerHandlers
  {
  }
}