using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanner.ProjectPlanRX;

namespace DirRX.ProjectPlanner.Client
{
  partial class ProjectPlanRXAnyChildEntityCollectionActions
  {
    public override void DeleteChildEntity(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      base.DeleteChildEntity(e);
    }

    public override bool CanDeleteChildEntity(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return !_objs.Any(x => x is IProjectPlanRXVersions);
    }

  }


  partial class ProjectPlanRXAnyChildEntityActions
  {
    public override void CopyChildEntity(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      base.CopyChildEntity(e);
    }

    public override bool CanCopyChildEntity(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return !(_obj is IProjectPlanRXVersions);
    }


    public override void AddChildEntity(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      base.AddChildEntity(e);
    }

    public override bool CanAddChildEntity(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return !(_obj is IProjectPlanRXVersions);
    }

  }

  partial class ProjectPlanRXVersionsActions
  {
    public override void ImportVersion(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      base.ImportVersion(e);
    }

    public override bool CanImportVersion(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return false;
    }

    public override void DeleteVersion(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      base.DeleteVersion(e);
      Functions.Module.Remote.DeleteProjectActiviesByNumberVersion(e.RootEntity.Id, _obj.Number.Value);
    }

    public override bool CanDeleteVersion(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return base.CanDeleteVersion(e);
    }

    
    public override void CreateVersion(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      base.CreateVersion(e);
    }

    public override bool CanCreateVersion(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return false;
    }

    public override void SendVersionByMail(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      base.SendVersionByMail(e);
    }

    public override bool CanSendVersionByMail(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return false;
    }


    public override void EditVersion(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      var webSite = DirRX.ProjectPlanner.PublicFunctions.Module.Remote.GetWebSite();
      DirRX.ProjectPlanner.PublicFunctions.Module.RunPlannerApp(webSite, _obj.RootEntity.Id, _obj.Number.Value, Users.Current.Id, false);
    }

    public override bool CanEditVersion(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      var webSiteStringEmpty = false;
      
      return !Functions.Module.Remote.VersionApproved(_obj.ElectronicDocument, _obj.Number.Value) &&
        _obj.ElectronicDocument.AccessRights.CanUpdate() && !Locks.GetLockInfo(_obj.ElectronicDocument).IsLockedByOther;
    }

    public override void ReadVersion(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      var webSite = DirRX.ProjectPlanner.PublicFunctions.Module.Remote.GetWebSite();
      DirRX.ProjectPlanner.PublicFunctions.Module.RunPlannerApp(webSite, _obj.RootEntity.Id, _obj.Number.Value, Users.Current.Id, true);
    }

    public override bool CanReadVersion(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return true;
    }

    public virtual bool CanCopyVersion(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return true;
    }

    public virtual void CopyVersion(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      Functions.Module.Remote.CreateCopyVersion(ProjectPlanRXes.As(e.RootEntity), _obj.Number.Value);
      var webSite = DirRX.ProjectPlanner.PublicFunctions.Module.Remote.GetWebSite();
      DirRX.ProjectPlanner.PublicFunctions.Module.RunPlannerApp(webSite, _obj.ElectronicDocument.Id, _obj.ElectronicDocument.LastVersion.Number.Value, Users.Current.Id, false);
    }

    public virtual bool CanImportVersionCustom(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return true;
    }

    public virtual void ImportVersionCustom(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      Functions.ProjectPlanRX.CreateProjectFromFileDialog(ProjectPlanRXes.As(_obj.ElectronicDocument), _obj.Number.Value);
    }

  }

  partial class ProjectPlanRXCollectionActions
  {
    public override void OpenDocumentRead(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var webSite = DirRX.ProjectPlanner.PublicFunctions.Module.Remote.GetWebSite();
      foreach (var project in _objs)
        DirRX.ProjectPlanner.PublicFunctions.Module.RunPlannerApp(webSite, project.Id, 0, Users.Current.Id, true);
    }

    public override bool CanOpenDocumentRead(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _objs.Select(x => x.HasVersions).Any(x => x);
    }

    public override void OpenDocumentEdit(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var webSite = DirRX.ProjectPlanner.PublicFunctions.Module.Remote.GetWebSite();
      foreach (var project in _objs)
      {
        try
        {
          var lockMessage = DirRX.ProjectPlanner.PublicFunctions.ProjectPlanRX.SetLockOnProjectPlan(project);
          if (string.IsNullOrEmpty(lockMessage))
            DirRX.ProjectPlanner.PublicFunctions.Module.RunPlannerApp(webSite, project.Id, project.LastVersion.Number.Value, Users.Current.Id, false);
          else
          {
            var dialog = Dialogs.CreateTaskDialog(string.Format(DirRX.ProjectPlanner.ProjectPlanRXes.Resources.ProjectPlanIsLocked,lockMessage, Environment.NewLine), MessageType.Question);
            dialog.Buttons.AddYesNo();
            
            if (dialog.Show() == DialogButtons.Yes)
              DirRX.ProjectPlanner.PublicFunctions.Module.RunPlannerApp(webSite, project.Id, project.LastVersion.Number.Value, Users.Current.Id, true);
          }          
        }
        catch (Exception ex)
        {
          Dialogs.ShowMessage(DirRX.ProjectPlanning.Projects.Resources.ErrorMessage, MessageType.Error);
          Logger.DebugFormat("{0} {1}", ex.Message, ex.InnerException != null ? ex.InnerException.Message : string.Empty);
        }
      }
      
    }

    public override bool CanOpenDocumentEdit(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return  _objs.Select(x => x.LastVersion != null && !Functions.Module.Remote.VersionApproved(x, x.LastVersion.Number.Value) && x.AccessRights.CanUpdate()).Any(x => x);
    }

  }


  partial class ProjectPlanRXActions
  {
    public override void ScanInNewVersion(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.ScanInNewVersion(e);
    }

    public override bool CanScanInNewVersion(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public override void CreateFromScanner(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.CreateFromScanner(e);
    }

    public override bool CanCreateFromScanner(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }


    public virtual void CreatePlanFromTemplate(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (string.IsNullOrEmpty(_obj.Name))
      {
        e.AddError(DirRX.ProjectPlanner.ProjectPlanRXes.Resources.NameCannotBeNull);
      }
      else
      {
        DirRX.ProjectPlanner.PublicFunctions.Module.Remote.WriteJsonBodyToProjectVersion(_obj, 0, false);
      }
    }

    public virtual bool CanCreatePlanFromTemplate(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.HasVersions && _obj.Stage != Stage.Completed;
    }

    public virtual bool CanCreatePlanFromFile(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.HasVersions && _obj.Stage != Stage.Completed;
    }

    public virtual void CreatePlanFromFile(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.ProjectPlanRX.CreateProjectFromFileDialog(_obj, 1);
    }

    public override void ShowSignatures(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.ShowSignatures(e);
    }

    public override bool CanShowSignatures(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanShowSignatures(e) && !_obj.State.IsInserted;
    }

    public override void CreateFromTemplate(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (string.IsNullOrEmpty(_obj.Name))
        e.AddError(DirRX.ProjectPlanner.ProjectPlanRXes.Resources.NameCannotBeNull);
      else
        DirRX.ProjectPlanner.PublicFunctions.Module.Remote.WriteJsonBodyToProjectVersion(_obj, 0, false);
    }

    public override bool CanCreateFromTemplate(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public virtual void ShowProject(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var project = Functions.ProjectPlanRX.Remote.GetLinkedProject(_obj);
      if(project != null)
        project.ShowModal();
      else
        Dialogs.ShowMessage(DirRX.ProjectPlanner.ProjectPlanRXes.Resources.ExistProjectError, MessageType.Warning);
    }

    public virtual bool CanShowProject(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsInserted;
    }

    public override void DeleteEntity(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.Module.Remote.DeleteProjectActivity(_obj);
      base.DeleteEntity(e);
    }

    public override bool CanDeleteEntity(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanDeleteEntity(e);
    }

    public override void CreateVersionFromLastVersion(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.CreateVersionFromLastVersion(e);
    }

    public override bool CanCreateVersionFromLastVersion(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public override void CopyEntity(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.CopyEntity(e);
    }

    public override bool CanCopyEntity(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanCopyEntity(e);
    }

    public override void CreateFromFile(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.ProjectPlanRX.CreateProjectFromFileDialog(_obj, 1);
    }

    public override bool CanCreateFromFile(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public virtual void CopyVersionFromLastVersion(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.Module.Remote.CreateCopyVersion(_obj, _obj.LastVersion.Number.Value);
      Dialogs.NotifyMessage("Создана новая версия проекта");
      
      var webSite = DirRX.ProjectPlanner.PublicFunctions.Module.Remote.GetWebSite();
      DirRX.ProjectPlanner.PublicFunctions.Module.RunPlannerApp(webSite, _obj.Id, _obj.LastVersion.Number.Value, Users.Current.Id, false);
    }

    public virtual bool CanCopyVersionFromLastVersion(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanCreateVersionFromLastVersion(e);
    }

    public virtual void EditLastVersion(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var webSite = DirRX.ProjectPlanner.PublicFunctions.Module.Remote.GetWebSite();
			if (string.IsNullOrEmpty(webSite))
      {
        Dialogs.ShowMessage(DirRX.ProjectPlanner.Resources.WebClientURIEmptyError, MessageType.Warning);
        return;
      }
      DirRX.ProjectPlanner.PublicFunctions.Module.RunPlannerApp(webSite, _obj.Id, _obj.LastVersion.Number.Value, Users.Current.Id, false);
    }

    public virtual bool CanEditLastVersion(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      var versoinApproved = _obj.LastVersion != null ? !Functions.Module.Remote.VersionApproved(_obj, _obj.LastVersion.Number.Value) : false;
      return !_obj.State.IsInserted && versoinApproved &&
        _obj.AccessRights.CanUpdate() && !Locks.GetLockInfo(_obj).IsLockedByOther && _obj.Stage != Stage.Completed;
    }

    public virtual void ReadLastVersion(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var webSite = DirRX.ProjectPlanner.PublicFunctions.Module.Remote.GetWebSite();
      DirRX.ProjectPlanner.PublicFunctions.Module.RunPlannerApp(webSite,  _obj.Id, _obj.LastVersion.Number.Value, Users.Current.Id, true);
    }

    public virtual bool CanReadLastVersion(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsInserted && _obj.HasVersions;
    }

    public override void SaveAndClose(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.SaveAndClose(e);
      
      if (_obj.IsCopy.HasValue && _obj.IsCopy.Value)
        DirRX.ProjectPlanner.Functions.Module.Remote.CreateCopyProject(_obj, _obj.LastVersion.Number.Value);
      else
      {
        if (_obj.HasVersions)
          DirRX.ProjectPlanner.PublicFunctions.Module.Remote.WriteJsonBodyToProjectVersion(_obj, _obj.LastVersion.Number.Value, false);
      }
    }

    public override bool CanSaveAndClose(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanSaveAndClose(e);
    }

    public override void Save(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.Save(e);
      
      if (_obj.IsCopy.HasValue && _obj.IsCopy.Value)
        DirRX.ProjectPlanner.Functions.Module.Remote.CreateCopyProject(_obj, _obj.LastVersion.Number.Value);
      else
      {
        if (_obj.HasVersions)
          DirRX.ProjectPlanner.PublicFunctions.Module.Remote.WriteJsonBodyToProjectVersion(_obj, _obj.LastVersion.Number.Value, false);
      }
    }

    public override bool CanSave(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanSave(e);
    }

  }

  

}