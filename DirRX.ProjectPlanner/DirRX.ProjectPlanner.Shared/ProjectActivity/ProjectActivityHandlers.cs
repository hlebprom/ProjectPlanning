using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanner.ProjectActivity;

namespace DirRX.ProjectPlanner
{
	partial class ProjectActivitySharedHandlers
	{

		public virtual void FactualCostsChanged(Sungero.Domain.Shared.DoublePropertyChangedEventArgs e)
		{
			if (e.NewValue.HasValue && e.NewValue != e.OldValue)
			{	
				_obj.ProjectPlan.FactualCosts = ((_obj.ProjectPlan.FactualCosts.HasValue ? _obj.ProjectPlan.FactualCosts.Value : 0.0)  - (e.OldValue.HasValue ? e.OldValue.Value : 0.0)) + e.NewValue;
			}
		}

		public virtual void PlannedCostsChanged(Sungero.Domain.Shared.DoublePropertyChangedEventArgs e)
		{
				_obj.ProjectPlan.PlannedCosts = ((_obj.ProjectPlan.PlannedCosts.HasValue ? _obj.ProjectPlan.PlannedCosts.Value : 0.0)  - (e.OldValue.HasValue ? e.OldValue.Value : 0.0)) + e.NewValue;
		}

		public virtual void PriorityChanged(Sungero.Domain.Shared.IntegerPropertyChangedEventArgs e)
		{
			if (e.NewValue.HasValue && e.NewValue != e.OldValue)
			{
				if (e.NewValue > 10 || e.NewValue < 1)
					_obj.Priority = 1;
			}
		}

		public virtual void NumberChanged(Sungero.Domain.Shared.IntegerPropertyChangedEventArgs e)
		{
			if (e.NewValue.HasValue && !e.NewValue.Equals(e.OldValue))
			{
				_obj.Number = e.NewValue;
				Functions.ProjectActivity.UpdateFullNumber(_obj);
			}
		}

	}
}