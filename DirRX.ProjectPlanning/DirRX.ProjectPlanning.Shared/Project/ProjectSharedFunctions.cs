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
			var haventProjectPlan = _obj.ProjectPlanDirRX == null;
			_obj.State.Properties.StartDate.IsEnabled = haventProjectPlan;
			_obj.State.Properties.EndDate.IsEnabled = haventProjectPlan;
			_obj.State.Properties.ActualStartDate.IsEnabled = haventProjectPlan;
			_obj.State.Properties.ActualFinishDate.IsEnabled = haventProjectPlan;
			_obj.State.Properties.BaselineWork.IsEnabled = haventProjectPlan;
			_obj.State.Properties.BaselineWorkType.IsEnabled = haventProjectPlan;
		}
	}
}