/* Copyright (c) Citrix Systems Inc. 
 * All rights reserved. 
 * 
 * Redistribution and use in source and binary forms, 
 * with or without modification, are permitted provided 
 * that the following conditions are met: 
 * 
 * *   Redistributions of source code must retain the above 
 *     copyright notice, this list of conditions and the 
 *     following disclaimer. 
 * *   Redistributions in binary form must reproduce the above 
 *     copyright notice, this list of conditions and the 
 *     following disclaimer in the documentation and/or other 
 *     materials provided with the distribution. 
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND 
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, 
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF 
 * MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE 
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR 
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, 
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, 
 * BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR 
 * SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, 
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING 
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE 
 * OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF 
 * SUCH DAMAGE.
 */

using System.Collections.Generic;
using System.Windows.Forms;
using XenAdmin.Actions;
using XenAdmin.Controls;
using XenAdmin.Dialogs;
using XenAPI;
using System.Linq;
using System.IO;
using XenAdmin.Alerts;

namespace XenAdmin.Wizards.PatchingWizard
{
    /// <summary>
    /// Remember that equals for patches dont work across connections because 
    /// we are not allow to override equals. YOU SHOULD NOT USE ANY OPERATION THAT IMPLIES CALL EQUALS OF Pool_path or Host_patch
    /// You should do it manually or use delegates.
    /// </summary>
    public partial class PatchingWizard : XenWizardBase
    {
        private readonly PatchingWizard_PatchingPage PatchingWizard_PatchingPage;
        private readonly PatchingWizard_SelectPatchPage PatchingWizard_SelectPatchPage;
        private readonly PatchingWizard_ModePage PatchingWizard_ModePage;
        private readonly PatchingWizard_SelectServers PatchingWizard_SelectServers;
        private readonly PatchingWizard_UploadPage PatchingWizard_UploadPage;
        private readonly PatchingWizard_PrecheckPage PatchingWizard_PrecheckPage;
        private readonly PatchingWizard_FirstPage PatchingWizard_FirstPage;
        private readonly PatchingWizard_AutomatedUpdatesPage PatchingWizard_AutomatedUpdatesPage;

        public PatchingWizard()
        {
            InitializeComponent();

            PatchingWizard_PatchingPage = new PatchingWizard_PatchingPage();
            PatchingWizard_SelectPatchPage = new PatchingWizard_SelectPatchPage();
            PatchingWizard_ModePage = new PatchingWizard_ModePage();
            PatchingWizard_SelectServers = new PatchingWizard_SelectServers();
            PatchingWizard_UploadPage = new PatchingWizard_UploadPage();
            PatchingWizard_PrecheckPage = new PatchingWizard_PrecheckPage();
            PatchingWizard_FirstPage = new PatchingWizard_FirstPage();
            PatchingWizard_AutomatedUpdatesPage = new PatchingWizard_AutomatedUpdatesPage();

            AddPage(PatchingWizard_FirstPage);
            AddPage(PatchingWizard_SelectPatchPage);
            AddPage(PatchingWizard_SelectServers);
            AddPage(PatchingWizard_UploadPage);
            AddPage(PatchingWizard_PrecheckPage);
            AddPage(PatchingWizard_ModePage);
            AddPage(PatchingWizard_PatchingPage);
        }

        public void AddAlert(XenServerPatchAlert alert)
        {
            PatchingWizard_SelectPatchPage.SelectDownloadAlert(alert);
            PatchingWizard_SelectPatchPage.SelectedUpdateAlert = alert;
            PatchingWizard_SelectServers.SelectedUpdateAlert = alert;
            PatchingWizard_UploadPage.SelectedUpdateAlert = alert;
        }

        public void AddFile(string path)
        {
            PatchingWizard_SelectPatchPage.AddFile(path);
        }

        public void SelectServers(List<Host> selectedServers)
        {
            PatchingWizard_SelectServers.SelectServers(selectedServers);
            PatchingWizard_SelectServers.DisableUnselectedServers();
        }

        protected override void UpdateWizardContent(XenTabPage senderPage)
        {
            var prevPageType = senderPage.GetType();

            if (prevPageType == typeof(PatchingWizard_SelectPatchPage))
            {
                var wizardIsInAutomatedUpdatesMode = PatchingWizard_SelectPatchPage.IsInAutomatedUpdatesMode;

                var updateType = wizardIsInAutomatedUpdatesMode ? UpdateType.NewRetail : PatchingWizard_SelectPatchPage.SelectedUpdateType;
                var newPatch = wizardIsInAutomatedUpdatesMode ? null : PatchingWizard_SelectPatchPage.SelectedNewPatch;
                var existPatch = wizardIsInAutomatedUpdatesMode ? null : PatchingWizard_SelectPatchPage.SelectedExistingPatch;
                var alertPatch = wizardIsInAutomatedUpdatesMode ? null : PatchingWizard_SelectPatchPage.SelectedUpdateAlert;
                var fileFromDiskAlertPatch = wizardIsInAutomatedUpdatesMode ? null : PatchingWizard_SelectPatchPage.FileFromDiskAlert;

                PatchingWizard_SelectServers.IsInAutomaticMode = wizardIsInAutomatedUpdatesMode;
                PatchingWizard_SelectServers.SelectedUpdateType = updateType;
                PatchingWizard_SelectServers.Patch = existPatch;
                PatchingWizard_SelectServers.SelectedUpdateAlert = alertPatch;
                PatchingWizard_SelectServers.FileFromDiskAlert = fileFromDiskAlertPatch;

                RemovePage(PatchingWizard_UploadPage);
                RemovePage(PatchingWizard_ModePage);
                RemovePage(PatchingWizard_PatchingPage);
                RemovePage(PatchingWizard_AutomatedUpdatesPage);
                if (!wizardIsInAutomatedUpdatesMode)
                {
                    AddAfterPage(PatchingWizard_SelectServers, PatchingWizard_UploadPage);
                    AddAfterPage(PatchingWizard_PrecheckPage, PatchingWizard_ModePage);
                    AddAfterPage(PatchingWizard_ModePage, PatchingWizard_PatchingPage);
                }
                else
                {
                    AddAfterPage(PatchingWizard_PrecheckPage, PatchingWizard_AutomatedUpdatesPage);
                }

                PatchingWizard_UploadPage.SelectedUpdateType = updateType;
                PatchingWizard_UploadPage.SelectedExistingPatch = existPatch;
                PatchingWizard_UploadPage.SelectedNewPatchPath = newPatch;
                PatchingWizard_UploadPage.SelectedUpdateAlert = alertPatch; 

                PatchingWizard_ModePage.Patch = existPatch;
                PatchingWizard_ModePage.SelectedUpdateAlert = alertPatch;

                PatchingWizard_PrecheckPage.IsInAutomatedUpdatesMode = wizardIsInAutomatedUpdatesMode;
                PatchingWizard_PrecheckPage.Patch = existPatch;
                PatchingWizard_PatchingPage.Patch = existPatch;

                PatchingWizard_PrecheckPage.SelectedUpdateType = updateType;
                
                PatchingWizard_ModePage.SelectedUpdateType = updateType;

                PatchingWizard_PatchingPage.SelectedUpdateType = updateType;
                PatchingWizard_PatchingPage.SelectedNewPatch = newPatch;
            }
            else if (prevPageType == typeof(PatchingWizard_SelectServers))
            {
                var selectedServers = PatchingWizard_SelectServers.SelectedServers;
                var selectedPools = PatchingWizard_SelectServers.SelectedPools;
                var selectedMasters = PatchingWizard_SelectServers.SelectedMasters;

                PatchingWizard_PrecheckPage.SelectedServers = selectedServers;

                PatchingWizard_ModePage.SelectedServers = selectedServers;

                PatchingWizard_PatchingPage.SelectedMasters = selectedMasters;
                PatchingWizard_PatchingPage.SelectedServers = selectedServers;
                PatchingWizard_PatchingPage.SelectedPools = selectedPools;

                PatchingWizard_UploadPage.SelectedMasters = selectedMasters;
                PatchingWizard_UploadPage.SelectedServers = selectedServers;

                PatchingWizard_AutomatedUpdatesPage.SelectedPools = selectedPools;
            }
            else if (prevPageType == typeof(PatchingWizard_UploadPage))
            {
                if (PatchingWizard_SelectPatchPage.SelectedUpdateType == UpdateType.NewRetail)
                {
                    PatchingWizard_SelectPatchPage.SelectedUpdateType = UpdateType.Existing;
                    PatchingWizard_SelectPatchPage.SelectedExistingPatch = PatchingWizard_UploadPage.Patch;

                    PatchingWizard_SelectServers.SelectedUpdateType = UpdateType.Existing;
                    PatchingWizard_SelectServers.Patch = PatchingWizard_UploadPage.Patch;

                    PatchingWizard_PrecheckPage.Patch = PatchingWizard_UploadPage.Patch;

                    PatchingWizard_ModePage.Patch = PatchingWizard_UploadPage.Patch;

                    PatchingWizard_PatchingPage.Patch = PatchingWizard_UploadPage.Patch;
                }
                PatchingWizard_PrecheckPage.PoolUpdate = PatchingWizard_UploadPage.PoolUpdate;
                PatchingWizard_PatchingPage.PoolUpdate = PatchingWizard_UploadPage.PoolUpdate;
                PatchingWizard_ModePage.PoolUpdate = PatchingWizard_UploadPage.PoolUpdate;
                PatchingWizard_PatchingPage.SuppPackVdis = PatchingWizard_UploadPage.SuppPackVdis;
            }
            else if (prevPageType == typeof(PatchingWizard_ModePage))
            {
                PatchingWizard_PatchingPage.ManualTextInstructions = PatchingWizard_ModePage.ManualTextInstructions;
                PatchingWizard_PatchingPage.IsAutomaticMode = PatchingWizard_ModePage.IsAutomaticMode;
                PatchingWizard_PatchingPage.RemoveUpdateFile = PatchingWizard_ModePage.RemoveUpdateFile;
            }
            else if (prevPageType == typeof(PatchingWizard_PrecheckPage))
            {
                PatchingWizard_PatchingPage.ProblemsResolvedPreCheck = PatchingWizard_PrecheckPage.ProblemsResolvedPreCheck;
                PatchingWizard_PatchingPage.LivePatchCodesByHost = PatchingWizard_PrecheckPage.LivePatchCodesByHost;
                PatchingWizard_ModePage.LivePatchCodesByHost = PatchingWizard_PrecheckPage.LivePatchCodesByHost;
                PatchingWizard_AutomatedUpdatesPage.ProblemsResolvedPreCheck = PatchingWizard_PrecheckPage.ProblemsResolvedPreCheck;
            }
        }

        private delegate List<AsyncAction> GetActionsDelegate();

        private List<AsyncAction> BuildSubActions(params GetActionsDelegate[] getActionsDelegate)
        {
            List<AsyncAction> result = new List<AsyncAction>();
            foreach (GetActionsDelegate getActionDelegate in getActionsDelegate)
            {
                var list = getActionDelegate();
                if (list != null && list.Count > 0)
                    result.AddRange(list);
            }
            return result;
        }

        private List<AsyncAction> GetUnwindChangesActions()
        {
            if (PatchingWizard_PrecheckPage.ProblemsResolvedPreCheck == null)
                return null;

            var actionList = (from problem in PatchingWizard_PrecheckPage.ProblemsResolvedPreCheck
                              where problem.SolutionActionCompleted
                              select problem.UnwindChanges());

            return actionList.Where(action => action != null &&
                                              action.Connection != null &&
                                              action.Connection.IsConnected).ToList();
        }

        private List<AsyncAction> GetRemovePatchActions(List<Pool_patch> patchesToRemove)
        {
            if (patchesToRemove == null)
                return null;

            List<AsyncAction> list = new List<AsyncAction>();
            foreach (Pool_patch patch in patchesToRemove)
            {
                if (patch.Connection != null && patch.Connection.IsConnected)
                {
                    if (patch.HostsAppliedTo().Count == 0)
                    {
                        list.Add(new RemovePatchAction(patch));
                    }
                    else
                    {
                        list.Add(new DelegatedAsyncAction(patch.Connection, Messages.REMOVE_PATCH, "", "", session => Pool_patch.async_pool_clean(session, patch.opaque_ref)));
                    }
                }
            }     
            return list;
        }

        private List<AsyncAction> GetRemovePatchActions()
        {
            return GetRemovePatchActions(PatchingWizard_UploadPage.NewUploadedPatches.Keys.ToList());
        }

        private List<AsyncAction> GetRemoveVdiActions(List<VDI> vdisToRemove)
        {
            if (vdisToRemove == null)
                return null;

            var list = (from vdi in vdisToRemove
                        where vdi.Connection != null && vdi.Connection.IsConnected
                        select new DestroyDiskAction(vdi));

            return list.OfType<AsyncAction>().ToList();
        }

        private List<AsyncAction> GetRemoveVdiActions()
        {
            return GetRemoveVdiActions(PatchingWizard_UploadPage.AllCreatedSuppPackVdis); ;
        }

        private void RunMultipleActions(string title, string startDescription, string endDescription,
            List<AsyncAction> subActions)
        {
            if (subActions.Count > 0)
            {
                using (MultipleAction multipleAction = new MultipleAction(xenConnection, title, startDescription,
                                                                          endDescription, subActions, false, true))
                {
                    using (var dialog = new ActionProgressDialog(multipleAction, ProgressBarStyle.Blocks))
                        dialog.ShowDialog(Program.MainWindow);
                }
            }
        }

        protected override void OnCancel()
        {
            base.OnCancel();

            List<AsyncAction> subActions = BuildSubActions(GetUnwindChangesActions, GetRemovePatchActions, GetRemoveVdiActions, GetCleanUpPoolUpdateActions);
            RunMultipleActions(Messages.REVERT_WIZARD_CHANGES, Messages.REVERTING_WIZARD_CHANGES,
                               Messages.REVERTED_WIZARD_CHANGES, subActions);

            RemoveDownloadedPatches();
        }

        private void RemoveUnwantedPatches(List<Pool_patch> patchesToRemove)
        {
            List<AsyncAction> subActions = GetRemovePatchActions(patchesToRemove);
            RunMultipleActions(Messages.PATCHINGWIZARD_REMOVE_UPDATES, Messages.PATCHINGWIZARD_REMOVING_UPDATES, Messages.PATCHINGWIZARD_REMOVED_UPDATES, subActions);
        }

        private void RemoveTemporaryVdis()
        {
            List<AsyncAction> subActions = GetRemoveVdiActions();
            RunMultipleActions(Messages.PATCHINGWIZARD_REMOVE_UPDATES, Messages.PATCHINGWIZARD_REMOVING_UPDATES, Messages.PATCHINGWIZARD_REMOVED_UPDATES, subActions);
        }

        private void CleanUpPoolUpdates()
        {
            var subActions = GetCleanUpPoolUpdateActions();
            RunMultipleActions(Messages.PATCHINGWIZARD_REMOVE_UPDATES, Messages.PATCHINGWIZARD_REMOVING_UPDATES, Messages.PATCHINGWIZARD_REMOVED_UPDATES, subActions);
        }

        private void RemoveDownloadedPatches()
        {
            var isInAutomaticMode = PatchingWizard_SelectPatchPage.IsInAutomatedUpdatesMode;

            List<string> listOfDownloadedFiles = new List<string>();

            if (isInAutomaticMode)
            {
                listOfDownloadedFiles.AddRange(PatchingWizard_AutomatedUpdatesPage.AllDownloadedPatches.Values);
            }
            else
            {
                listOfDownloadedFiles.AddRange(PatchingWizard_UploadPage.AllDownloadedPatches.Values);
            }

            foreach (string downloadedPatch in listOfDownloadedFiles)
            {
                try
                {
                    if (File.Exists(downloadedPatch))
                    {
                        File.Delete(downloadedPatch);
                    }
                }
                catch
                {  
                    log.DebugFormat("Could not remove downloaded patch {0} ", downloadedPatch);
                }
            }           
        }

        protected override void FinishWizard()
        {
            if (PatchingWizard_UploadPage.NewUploadedPatches != null)
            {
                List<Pool_patch> patchesToRemove =
                    PatchingWizard_UploadPage.NewUploadedPatches.Keys.ToList().Where(
                        patch => !string.Equals(patch.uuid, PatchingWizard_UploadPage.Patch.uuid, System.StringComparison.OrdinalIgnoreCase)).ToList();

                RemoveUnwantedPatches(patchesToRemove);
            }

            if (PatchingWizard_UploadPage.AllCreatedSuppPackVdis != null)
                RemoveTemporaryVdis();

            CleanUpPoolUpdates();

            RemoveDownloadedPatches();

            base.FinishWizard();
        }

        private List<AsyncAction> GetCleanUpPoolUpdateActions()
        {
            if (PatchingWizard_UploadPage.AllIntroducedPoolUpdates != null && PatchingWizard_UploadPage.AllIntroducedPoolUpdates.Count > 0)
            {
                return PatchingWizard_UploadPage.AllIntroducedPoolUpdates.Select(GetCleanUpPoolUpdateAction).ToList();
            }

            return new List<AsyncAction>();
        }

        private static AsyncAction GetCleanUpPoolUpdateAction(Pool_update poolUpdate)
        {
            return
                new DelegatedAsyncAction(poolUpdate.Connection, Messages.REMOVE_PATCH, "", "", session =>
                {
                    try
                    {
                        Pool_update.pool_clean(session, poolUpdate.opaque_ref);
                        if(!poolUpdate.AppliedOnHosts.Any())
                            Pool_update.destroy(session, poolUpdate.opaque_ref);
                    }
                    catch (Failure f)
                    {
                        log.Error("Clean up failed", f);
                    }
                });
        }
    }
}
