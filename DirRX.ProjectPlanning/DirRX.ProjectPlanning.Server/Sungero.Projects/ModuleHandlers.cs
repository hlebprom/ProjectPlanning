using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.ProjectPlanning.Module.Projects.Server
{
	partial class ProjectsPlansDirRXFolderHandlers
	{

		public virtual IQueryable<DirRX.ProjectPlanner.IProjectPlanRX> ProjectsPlansDirRXDataQuery(IQueryable<DirRX.ProjectPlanner.IProjectPlanRX> query)
		{
			if (_filter == null)
				return query;
			
			// Фильтр по состоянию.
			if (_filter.Active || _filter.Closed || _filter.Closing || _filter.Initiation)
				query = query.Where(x => (_filter.Active && x.Stage == DirRX.ProjectPlanner.ProjectPlanRX.Stage.Execution) ||
				                    (_filter.Closed && x.Stage == DirRX.ProjectPlanner.ProjectPlanRX.Stage.Completed) ||
				                    (_filter.Closing && x.Stage == DirRX.ProjectPlanner.ProjectPlanRX.Stage.Completion) ||
				                    (_filter.Initiation && x.Stage == DirRX.ProjectPlanner.ProjectPlanRX.Stage.Initiation));

			var today = Calendar.UserToday;
			
			// Фильтр по дате начала проекта.
			var startDateBeginPeriod = _filter.StartDateRangeFrom ?? Calendar.SqlMinValue;
			var startDateEndPeriod = _filter.StartDateRangeTo ?? Calendar.SqlMaxValue;
			
			if (_filter.StartPeriodThisMonth)
			{
				startDateBeginPeriod = today.BeginningOfMonth();
				startDateEndPeriod = today.EndOfMonth();
			}
			
			if (_filter.StartPeriodThisMonth || (_filter.StartDateRangeFrom != null || _filter.StartDateRangeTo != null))
				query = query.Where(x => (x.StartDate.Between(startDateBeginPeriod, startDateEndPeriod) && !Equals(x.Stage, DirRX.ProjectPlanner.ProjectPlanRX.Stage.Completed)) ||
				                    (x.ActualStartDate.Between(startDateBeginPeriod, startDateEndPeriod) && Equals(x.Stage, DirRX.ProjectPlanner.ProjectPlanRX.Stage.Completed)));

			// Фильтр по дате окончания проекта.
			var finishDateBeginPeriod = _filter.FinishDateRangeFrom ?? Calendar.SqlMinValue;
			var finishDateEndPeriod = _filter.FinishDateRangeTo ?? Calendar.SqlMaxValue;
			
			if (_filter.FinishPeriodThisMonth)
			{
				finishDateBeginPeriod = today.BeginningOfMonth();
				finishDateEndPeriod = today.EndOfMonth();
			}
			
			if (_filter.FinishPeriodThisMonth || (_filter.FinishDateRangeFrom != null || _filter.FinishDateRangeTo != null))
				query = query.Where(x => (x.EndDate.Between(finishDateBeginPeriod, finishDateEndPeriod) && !Equals(x.Stage, DirRX.ProjectPlanner.ProjectPlanRX.Stage.Completed)) ||
				                    (x.ActualFinishDate.Between(finishDateBeginPeriod, finishDateEndPeriod) && Equals(x.Stage, DirRX.ProjectPlanner.ProjectPlanRX.Stage.Completed)));
			
			return query;
		}
	}

	partial class ProjectsHandlers
	{
	}
}