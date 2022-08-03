using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanner.ProjectPlanRX;

namespace DirRX.ProjectPlanner.Shared
{
  partial class ProjectPlanRXFunctions
  {
		/// <summary>
		/// Установить доступность свойств в зависимости от наличия этапов по проекту.
		/// </summary>
		public void SetPropertiesAvailability()
		{
			// TODO: используется серверная функция для получения списка проектов.
			var haveActivities = ProjectPlanner.PublicFunctions.ProjectActivity.Remote.GetActivities(_obj).Any();
			_obj.State.Properties.StartDate.IsEnabled = !haveActivities;
			_obj.State.Properties.EndDate.IsEnabled = !haveActivities;
			_obj.State.Properties.BaselineWork.IsEnabled = !haveActivities;
			_obj.State.Properties.BaselineWorkType.IsEnabled = !haveActivities;
			_obj.State.Properties.PlannedCosts.IsEnabled = false;
			_obj.State.Properties.FactualCosts.IsEnabled = false;
		}
  }
}