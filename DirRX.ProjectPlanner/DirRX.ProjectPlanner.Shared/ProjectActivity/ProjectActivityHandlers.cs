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
		  //Kiselev_EM HACK #gantt-971 для отслеживания Bug 173239, решено явно проверить на null и залогировать
		  //потенциальные объекты приводящие к ошибке.
		  if (_obj == null)
		  {
		    Logger.Error(DirRX.ProjectPlanner.ProjectActivities.Resources.FactualCostChangeEmptyObjText);
		    return;
		  }
		  
		  if (e == null)
		  {
		    Logger.Error(DirRX.ProjectPlanner.ProjectActivities.Resources.FactualCostChangeEmptyArgsText);
		    return;
		  }
		  
		  if (_obj.ProjectPlan == null)
		  {
		    Logger.Error(DirRX.ProjectPlanner.ProjectActivities.Resources.FactualCostChangeEmptyProjectPlanText);
		    return;
		  }
		  
		  var oldValue = e.OldValue.HasValue ? e.OldValue.Value : 0.0;
		  var newValue = e.NewValue.HasValue ? e.NewValue.Value : 0.0;
		  if (oldValue != newValue)
		  {
		    _obj.ProjectPlan.FactualCosts = ((_obj.ProjectPlan.FactualCosts.HasValue ? _obj.ProjectPlan.FactualCosts.Value : 0.0) - oldValue) + newValue;
		  }
		}

		public virtual void PlannedCostsChanged(Sungero.Domain.Shared.DoublePropertyChangedEventArgs e)
		{
		  _obj.ProjectPlan.PlannedCosts = ((_obj.ProjectPlan.PlannedCosts.HasValue ? _obj.ProjectPlan.PlannedCosts.Value : 0.0)  - (e.OldValue.HasValue ? e.OldValue.Value : 0.0)) + (e.NewValue.HasValue ? e.NewValue.Value : 0.0);
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