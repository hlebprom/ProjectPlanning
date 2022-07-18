using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanner.ProjectPlan;

namespace DirRX.ProjectPlanner
{

	partial class ProjectPlanTeamMembersSharedCollectionHandlers
	{

		public virtual void TeamMembersAdded(Sungero.Domain.Shared.CollectionPropertyAddedEventArgs e)
		{
			if (_added.Group == null)
				_added.Group = ProjectPlanTeamMembers.Group.Change;
		}
	}

	partial class ProjectPlanSharedHandlers
	{

		public virtual void ExecutionPercentChanged(Sungero.Domain.Shared.IntegerPropertyChangedEventArgs e)
		{
			if (e.NewValue != e.OldValue)
			{
				var linkedProject = Functions.ProjectPlan.Remote.GetLinkedProejct(_obj);
				if (linkedProject != null)
					linkedProject.ExecutionPercent = e.NewValue;
			}
		}

		public virtual void EndDateChanged(Sungero.Domain.Shared.DateTimePropertyChangedEventArgs e)
		{
			if (e.NewValue != e.OldValue)
			{
				var linkedProject = Functions.ProjectPlan.Remote.GetLinkedProejct(_obj);
				if (linkedProject != null)
					linkedProject.EndDate = e.NewValue;
			}
		}

		public virtual void StartDateChanged(Sungero.Domain.Shared.DateTimePropertyChangedEventArgs e)
		{
			if (e.NewValue != e.OldValue)
			{
				var linkedProject = Functions.ProjectPlan.Remote.GetLinkedProejct(_obj);
				if (linkedProject != null)
					linkedProject.StartDate = e.NewValue;
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
				var linkedProject = Functions.ProjectPlan.Remote.GetLinkedProejct(_obj);
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
				var linkedProject = Functions.ProjectPlan.Remote.GetLinkedProejct(_obj);
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