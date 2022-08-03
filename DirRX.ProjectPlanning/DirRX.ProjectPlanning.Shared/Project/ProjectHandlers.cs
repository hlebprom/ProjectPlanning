using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanning.Project;

namespace DirRX.ProjectPlanning
{


  partial class ProjectSharedHandlers
  {

    public virtual void BaselineWorkTypeChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      if (e.NewValue != e.OldValue)
      {
        if (_obj.ProjectPlanDirRX != null)
          _obj.ProjectPlanDirRX.BaselineWorkType = e.NewValue;
      }
    }

    public override void ActualFinishDateChanged(Sungero.Domain.Shared.DateTimePropertyChangedEventArgs e)
    {
      base.ActualFinishDateChanged(e);
    }

    public override void ActualStartDateChanged(Sungero.Domain.Shared.DateTimePropertyChangedEventArgs e)
    {
      base.ActualStartDateChanged(e);
    }

    public override void EndDateChanged(Sungero.Domain.Shared.DateTimePropertyChangedEventArgs e)
    {
      base.EndDateChanged(e);
    }

    public override void StartDateChanged(Sungero.Domain.Shared.DateTimePropertyChangedEventArgs e)
    {
      base.StartDateChanged(e);
    }

    public override void ShortNameChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      base.ShortNameChanged(e);
    }

    public override void NameChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      base.NameChanged(e);
      if (e.NewValue != e.OldValue)
      {
        if (_obj.ProjectPlanDirRX != null && !_obj.State.IsCopied && !e.NewValue.StartsWith(DirRX.ProjectPlanning.Projects.Resources.TitleProjectPlan))
        {
          _obj.ProjectPlanDirRX.Name = string.Format(DirRX.ProjectPlanning.Projects.Resources.TitleProjectPlan, e.NewValue);
        }
      }
    }

    public virtual void ProjectPlanDirRXChanged(DirRX.ProjectPlanning.Shared.ProjectProjectPlanDirRXChangedEventArgs e)
    {
      if (e.NewValue != null && e.NewValue != e.OldValue && !e.NewValue.State.IsInserted)
      {
        if(!_obj.State.IsInserted)
        {
          _obj.Folder.Items.Remove(e.OldValue);
          _obj.Folder.Items.Add(e.NewValue);
        }
        
        _obj.StartDate = e.NewValue.StartDate;
        _obj.ActualStartDate = e.NewValue.ActualStartDate;
        _obj.ActualFinishDate = e.NewValue.ActualFinishDate;
        _obj.EndDate = e.NewValue.EndDate;
        _obj.FactualCosts = e.NewValue.FactualCosts;
        _obj.PlannedCosts = e.NewValue.PlannedCosts;
        _obj.ExecutionPercent = e.NewValue.ExecutionPercent;
        
        foreach (var tm in e.NewValue.TeamMembers)
        {
          if (!_obj.TeamMembers.Any(x => Recipients.Equals(x.Member, tm.Member)))
          {
            var newRow = _obj.TeamMembers.AddNew();
            newRow.Member = tm.Member;
            newRow.Group = tm.Group;
          }
        }
      }
      else if (e.NewValue == null)
      {
        if (e.OldValue != null)
          _obj.Folder.Items.Remove(e.OldValue);
        if (e.OriginalValue != null)
          _obj.Folder.Items.Remove(e.OriginalValue);
      }
      
    }

    public virtual void PlannedCostsChanged(Sungero.Domain.Shared.DoublePropertyChangedEventArgs e)
    {
      if (e.NewValue.HasValue && e.NewValue < 0.0)
        _obj.PlannedCosts = e.OriginalValue;
    }

    public virtual void FactualCostsChanged(Sungero.Domain.Shared.DoublePropertyChangedEventArgs e)
    {
      if (e.NewValue.HasValue && e.NewValue < 0.0)
        _obj.FactualCosts = e.OriginalValue;
    }
  }


  
}