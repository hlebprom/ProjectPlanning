using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanner.ProjectPlanRX;

namespace DirRX.ProjectPlanner
{
  partial class ProjectPlanRXTeamMembersSharedCollectionHandlers
  {
    public virtual void TeamMembersAdded(Sungero.Domain.Shared.CollectionPropertyAddedEventArgs e)
    {
      if (_added.Group == null)
        _added.Group = ProjectPlanRXTeamMembers.Group.Change;
    }
  }

  partial class ProjectPlanRXSharedHandlers
  {

    public override void DocumentKindChanged(Sungero.Docflow.Shared.OfficialDocumentDocumentKindChangedEventArgs e)
    {
      if (_obj.Name != null && !_obj.Name.StartsWith(Resources.ProjectPlanName))
        base.DocumentKindChanged(e);
    }

    public virtual void ExecutionPercentChanged(Sungero.Domain.Shared.IntegerPropertyChangedEventArgs e)
    {
      if (e.NewValue != e.OldValue)
      {
        var linkedProject = Functions.ProjectPlanRX.Remote.GetLinkedProject(_obj);
        if (linkedProject != null)
          linkedProject.ExecutionPercent = e.NewValue;
      }
    }

    public virtual void FactualCostsChanged(Sungero.Domain.Shared.DoublePropertyChangedEventArgs e)
    {
      
      if (e.NewValue.HasValue && e.NewValue < 0.0)
      {
        _obj.FactualCosts = e.OriginalValue;
      }
      
      if (e.NewValue != e.OldValue)
      {
        var linkedProject = Functions.ProjectPlanRX.Remote.GetLinkedProject(_obj);
        if (linkedProject != null)
          linkedProject.FactualCosts = e.NewValue;
      }
    }

    public virtual void PlannedCostsChanged(Sungero.Domain.Shared.DoublePropertyChangedEventArgs e)
    {

      if (e.NewValue.HasValue && e.NewValue < 0.0)
        _obj.PlannedCosts = e.OriginalValue;
      
      if (e.NewValue != e.OldValue)
      {
        var linkedProject = Functions.ProjectPlanRX.Remote.GetLinkedProject(_obj);
        if (linkedProject != null)
          linkedProject.PlannedCosts = e.NewValue;
      }
    }

    public virtual void StageChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      if (e.NewValue != Stage.Completed)
        _obj.Status = Sungero.CoreEntities.DatabookEntry.Status.Active;
      else
        _obj.Status = Sungero.CoreEntities.DatabookEntry.Status.Closed;
    }

  }
}