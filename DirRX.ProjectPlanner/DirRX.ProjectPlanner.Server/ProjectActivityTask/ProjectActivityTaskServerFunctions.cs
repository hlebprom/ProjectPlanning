using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanner.ProjectActivityTask;
using PplanMessages;
using Sungero.Domain.Clients;

namespace DirRX.ProjectPlanner.Server
{
  partial class ProjectActivityTaskFunctions
  {
    [Remote]
    public static void SendTaskStarted(IProjectActivityTask task)
    {
      var clientId = task.ProjectPlan.AccessRights.Current.Where(ar => Users.Is(ar.Recipient)).Select(ar => Users.As(ar.Recipient)).ToList()
        .SelectMany(user => ClientManager.Instance.GetClientsOfUser(user.Id))
        .Distinct()
        .ToArray();
      if (task.MaxDeadline == null)
      {
        task.MaxDeadline = Calendar.Now;
      }
      PplanMessages.PplanMessageSender.SendTaskAdded(clientId, "gantt", task.ProjectPlan.Id, task.ProjectActivity.NumberVersion.Value, task.ProjectActivity.Id, task.Id, task.DisplayValue, Hyperlinks.Get(task), task.MaxDeadline.ToUserTime().Value.Ticks);
    }
    
    [Remote]
    public static void SendTaskEnded(IProjectActivityTask task)
    {
      var clientId = task.ProjectPlan.AccessRights.Current.Where(ar => Users.Is(ar.Recipient)).Select(ar => Users.As(ar.Recipient)).ToList()
        .SelectMany(user => ClientManager.Instance.GetClientsOfUser(user.Id))
        .Distinct()
        .ToArray();
      PplanMessages.PplanMessageSender.SendTaskFinished(clientId, "gantt", task.ProjectPlan.Id, task.ProjectActivity.NumberVersion.Value, task.ProjectActivity.Id, task.Id, Hyperlinks.Get(task));
    }
    
  }
}