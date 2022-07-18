using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanning.Project;

namespace DirRX.ProjectPlanning.Shared
{
	partial class ProjectFunctions
	{

		/// <summary>
		/// Установить доступность свойств в зависимости от наличия этапов по проекту.
		/// </summary>
		public void SetPropertiesAvailability()
		{
			var haveProjectPlan = _obj.ProjectPlanDirRX == null;
			_obj.State.Properties.StartDate.IsEnabled = haveProjectPlan;
			_obj.State.Properties.EndDate.IsEnabled = haveProjectPlan;
			_obj.State.Properties.ActualStartDate.IsEnabled = haveProjectPlan;
			_obj.State.Properties.ActualFinishDate.IsEnabled = haveProjectPlan;
			_obj.State.Properties.BaselineWork.IsEnabled = haveProjectPlan;
			_obj.State.Properties.BaselineWorkType.IsEnabled = haveProjectPlan;
			_obj.State.Properties.ExecutionPercent.IsEnabled = false;
			_obj.State.Properties.PlannedCosts.IsEnabled = false;
			_obj.State.Properties.FactualCosts.IsEnabled = false;
		}
	}
}