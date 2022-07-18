using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.ProjectPlanner.Structures.Module
{
  [Public]
  partial class ResourcesWorkloadDto
  {
    public int ResourceId { get; set; }
    public double Value { get; set; }
  }
  
  [Public]
  partial class PredecessorDto
  {
    public int? Id { get; set; }
    public string LinkType { get; set; }
  }
  
  [Public]
  partial class TaskDto
  {
    public int? Id { get; set; }
    public string DisplayValue { get; set; }
    public string HyperLink { get; set; }
    public DateTime? Deadline { get; set; }
  }
  
  [Public]
  partial class ActivityStatusDto
  {
    public string EnumValue { get; set; }
    public string LocalizeValue { get; set; }
  }
  
  [Public]
  partial class ProjectDto
  {
    public double? BaselineWork { get; set; }
    public DateTime? EndDate { get; set; }
    public int? ExecutionPercent { get; set; }
    public int Id { get; set; }
    public string Name { get; set; }
    public string Note { get; set; }
    public DateTime? StartDate { get; set; }
    public string TypeActivity { get; set; }
    public double? PlannedCosts { get; set; }
    public double? FactualCosts { get; set; }
    public int? ManagerId { get; set; }
  }
  
  [Public]
  partial class ResourceTypesDto
  {
    public int Id { get; set; }
    public string Name { get; set; }
    public string SectionName { get; set; }
  }
  
  [Public]
  partial class UsersDto
  {
    public int? Id { get; set; }
    public string Name { get; set; }
    public string Surname { get; set; }
    public string Position { get; set; }
  }
  
  [Public]
  partial class MaterialResourcesDto
  {
    public int Id { get; set; }
    public string Name { get; set; }
  }
  
  [Public]
  partial class ResourcesDto
  {
    public int Id { get; set; }
    public int EntityTypeId { get; set; }
    public int EntityId { get; set; }
    public string UnitLabel { get; set; }
  }
  
  [Public]
  partial class CapacityDto
  {
    public int ResourceId { get; set; }
    public List<DirRX.ProjectPlanner.Structures.Module.ICapacityValueDto> Values { get; set; }
  }
  
  [Public]
  partial class CapacityValueDto
  {
    public DateTime Date { get; set; }
    public double Busy { get; set; }
  }
  
  [Public]
  partial class WorkingTimeCalendarDto
  {
    public List<int> ResourcesIds { get; set; }
    public List<DateTime> FreeDays { get; set; }
    public List<DirRX.ProjectPlanner.Structures.Module.IWorkDaysDto> WorkDays { get; set; }
  }
  
  [Public]
  partial class WorkDaysDto
  {
    public DateTime Date { get; set; }
    public double Duration { get; set; }
  }
  
  [Public]
  partial class ResourcesDataDto
  {
    public List<DirRX.ProjectPlanner.Structures.Module.IResourceTypesDto> ResourceTypes { get; set; }
    public List<DirRX.ProjectPlanner.Structures.Module.IUsersDto> Users { get; set; }
    public List<DirRX.ProjectPlanner.Structures.Module.IMaterialResourcesDto> MaterialResources { get; set; }
    public List<DirRX.ProjectPlanner.Structures.Module.IResourcesDto> Resources { get; set; }
    public List<DirRX.ProjectPlanner.Structures.Module.ICapacityDto> Capacity { get; set; }
    public List<DirRX.ProjectPlanner.Structures.Module.IWorkingTimeCalendarDto> WorkingTimeCalendars { get; set; }
  }
  
  [Public]
  partial class ActivityDto
  {
    public int? Priority { get; set; }
    public double? FactualCosts { get; set; }
    public double? PlannedCosts { get; set; }
    public string TypeActivity { get; set; }
    public int SortIndex { get; set; }
    public List<DirRX.ProjectPlanner.Structures.Module.ITaskDto> UnfinishedTasks { get; set; }
    public List<DirRX.ProjectPlanner.Structures.Module.ITaskDto> SubmittedTasks { get; set; }
    public List<DirRX.ProjectPlanner.Structures.Module.IPredecessorDto> Predecessors { get; set; }
    public DirRX.ProjectPlanner.Structures.Module.IActivityStatusDto Status { get; set; }
    public int ExecutionPercent { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime StartDate { get; set; }
    public int? ResponsibleId { get; set; }
    public int? LeadActivityId { get; set; }
    public string Note { get; set; }
    public int? CurrentNumber { get; set; }
    public string Name { get; set; }
    public int Id { get; set; }
    public double? BaselineWork { get; set; }
    public List<DirRX.ProjectPlanner.Structures.Module.IResourcesWorkloadDto> Resources { get; set; }
  }
  
  [Public]
  partial class GanttProjectPlanDto
  {
    public List<DirRX.ProjectPlanner.Structures.Module.IActivityDto> Activities { get; set; }
    public int LastActivityId { get; set; }
    public DirRX.ProjectPlanner.Structures.Module.IResourcesDataDto ResourcesData { get; set; }
    public DirRX.ProjectPlanner.Structures.Module.IProjectDto Project { get; set; }
    //public List<DirRX.ProjectPlanner.Structures.Module.IActivityStatusDto> ActivityStatuses { get; set; }
    public int NumberVersion { get; set; }
    public int ProjectPlanId { get; set; }
  }
  
  /// <summary>
  /// Результат запроса на поиск получателей
  /// </summary>
  [Public]
  partial class SearchRecipientsResult
  {
    public List<DirRX.ProjectPlanner.Structures.Module.IRecipientDto> Recipients { get; set; }
  }
  
  /// <summary>
  /// Результат запроса проверки существования плана.
  /// </summary>
  [Public]
  partial class CheckPlanExistsResult
  {
    public bool IsExists { get; set; }
  }

  /// <summary>
  /// Данные о получателе
  /// </summary>
  [Public]
  partial class RecipientDto
  {
    public int Id { get; set; }
    public string Name { get; set; }
    public string Thumbnail { get; set; }
  }

  /// <summary>
  /// 
  /// </summary>
  [Public]
  partial class ProjectPlanDto
  {
    public DirRX.ProjectPlanner.Structures.Module.IResourcesData ResourceData  { get; set; }
    public List<DirRX.ProjectPlanner.Structures.Module.ITask> Tasks { get; set; }
    public string BaselineWorkType { get; set; }
  }

  /// <summary>
  /// 
  /// </summary>
 [Public]
  partial class Task
  {
    public int ActivityId { get; set; }
    public DateTime Deadline { get; set; }
    public string HyperLink { get; set; }
    public int Id { get; set; }
    public string TaskStatus { get; set; }
    public string DisplayValue { get; set; }
  }

  /// <summary>
  /// 
  /// </summary>
  [Public]
  partial class User
  {
    public int Id { get; set; }
    public string Name { get; set; }
  }

  /// <summary>
  /// 
  /// </summary>
  [Public]
  partial class ResourceTypes
  {
    public int Id { get; set; }
    public string Name { get; set; }
    public string SectionName { get; set; }
  }

  /// <summary>
  /// 
  /// </summary>
  [Public]
  partial class Resource
  {
    public int Id { get; set; }
    public int EntityTypeId { get; set; }
    public int EntityId { get; set; }
    public string UnitLabel { get; set; }
  }

  /// <summary>
  /// 
  /// </summary>
  [Public]
  partial class MaterialResource
  {
    public int Id { get; set; }
    public string Name { get; set; }
  }

  /// <summary>
  /// 
  /// </summary>
  [Public]
  partial class ResourcesData
  {
    public List<DirRX.ProjectPlanner.Structures.Module.ICapacity> Capacity { get; set; }
    public List<DirRX.ProjectPlanner.Structures.Module.IMaterialResource> MaterialResources { get; set; }
    public List<DirRX.ProjectPlanner.Structures.Module.IResource> Resources { get; set; }
    public List<DirRX.ProjectPlanner.Structures.Module.IResourceTypes> ResourceTypes { get; set; }
    public List<DirRX.ProjectPlanner.Structures.Module.IUser> Users { get; set; }
    public List<DirRX.ProjectPlanner.Structures.Module.IWorkingTimeCalendar> WorkingTimeCalendars { get; set; }
  }

  /// <summary>
  /// 
  /// </summary>
  [Public]
  partial class WorkDay
  {
    public DateTime Date { get; set; }
    public double Duration { get; set; }
  }

  /// <summary>
  /// 
  /// </summary>
  [Public]
  partial class CapacityValue
  {
    public DateTime Date { get; set; }
    public double Busy { get; set; }
  }

  /// <summary>
  /// 
  /// </summary>
  [Public]
  partial class Capacity
  {
    public int ResourceId { get; set; }
    public List<DirRX.ProjectPlanner.Structures.Module.ICapacityValue> Values { get; set; }
  }

  /// <summary>
  /// 
  /// </summary>
  [Public]
  partial class WorkingTimeCalendar
  {
    public List<int> ResourcesIds { get; set; }
    public List<DateTime> FreeDays { get; set; }
    public List<DirRX.ProjectPlanner.Structures.Module.IWorkDay> WorkDays { get; set; }
  }

  /// <summary>
  /// 
  /// </summary>
  [Public]
  partial class CapacityResponseDto
  {
    public List<DirRX.ProjectPlanner.Structures.Module.IWorkingTimeCalendar> WorkingTimeCalendar { get; set; }
    public List<DirRX.ProjectPlanner.Structures.Module.ICapacity> Capacity { get; set; }
  }
  
  partial class ProjectPlanDates
  {
    public DateTime Start {get; set;}
    
    public DateTime End {get; set;}
  }
 
  /// <summary>
  /// Соответствие этапа и ИД ведущего этапа по модели.
  /// </summary>
  partial class LeadActivities
  {
    public DirRX.ProjectPlanner.IProjectActivity Activity { get; set; }
    
    public int LeadActivityId { get; set; }
  }
  
  /// <summary>
  /// Соответствие ИД новых этапов из модели и базы.
  /// </summary>
  partial class ModelActivity
  {
    public int ModelActivitylId { get; set; }
    
    public int ID { get; set; }
  }
  
  /// <summary>
  /// Средняя нагрузка по периоду
  /// </summary>
  partial class AverageBusy
  {
    public DateTime StartDate {get;set;}
    
    public DateTime EndDate {get;set;}
    
    public double AvgBusy {get;set;}
  }
}