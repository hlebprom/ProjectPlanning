using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.ProjectPlanner.Structures.Module
{
 
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
  partial class IDActivities
  {
    public int IDApp { get; set; }
    
    public int ID { get; set; }
  }
}