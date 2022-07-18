using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.ProjectPlanner.Shared
{
  public class ModuleFunctions
  {

    /// <summary>
    /// Возвращает количество рабочих дней в периоде между датами.
    /// </summary>
    /// <param name="startDate">Дата начала периода.</param>
    /// <param name="endDate">Дата конца периода.</param>
    /// <returns>Количество рабочих дней в периоде между датами.</returns>
    [Public]
    public int GetWorkiningDaysInPeriod(DateTime startDate, DateTime endDate)
    {
      var res = 0;
      var curDate = startDate;
      while (curDate <= endDate)
      {
        if (Calendar.IsWorkingDay(curDate))
          res++;
        curDate = curDate.AddDays(1);
      }
      return res;
    }

  }
}