﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using VMS.TPS.Common.Model.API;

namespace Checklist
{
    public partial class Checklist
    {
        private Patient patient;
        private Course course;
        private PlanSetup planSetup;
        private ChecklistType checklistType;
        private string userId;

        private Fractionation fractionation;
        private StructureSet structureSet;
        private Image image;
        
        private TreatmentUnitManufacturer treatmentUnitManufacturer;
        private TreatmentSide treatmentSide;
        private int numberOfBeams;
        private int numberOfTreatmentBeams;
        
        private long patientSer = -1;
        private long courseSer = -1;
        private long planSetupSer = -1;

        private List<ChecklistItem> checklistItems = new List<ChecklistItem>();
        private DatabaseManager databaseManager = new DatabaseManager(Settings.RESULT_SERVER, Settings.RESULT_USERNAME, Settings.RESULT_PASSWORD);

        public Checklist(Patient patient, Course course, PlanSetup planSetup, ChecklistType checklistType, string userId)
        {
            //planSetup.Beams = planSetup.Beams.OrderByDescending(x => x.Id);
            this.patient = patient;
            this.course = course;
            this.planSetup = planSetup;
            this.checklistType = checklistType;
            this.userId = userId;

            try
            {
                databaseManager.CreateDatabase();
            }
            catch
            {
            }

            try
            {
                structureSet = planSetup.StructureSet;
                if (structureSet != null)
                    image = planSetup.StructureSet.Image;
                try { fractionation = planSetup.UniqueFractionation; }
                catch { }

                treatmentUnitManufacturer = GetTreatmentUnitManufacturer();
                treatmentSide = GetTreatmentSide(planSetup);
                numberOfTreatmentBeams = GetNumberOfTreatmentBeams();
                numberOfBeams = GetNumberOfBeams();
            }
            catch (Exception exception)
            {
                System.Windows.Forms.MessageBox.Show(exception.Message, "Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }

            try
            {
                AriaInterface.Connect();
                AriaInterface.GetPlanSetupSer(patient.Id, course.Id, planSetup.Id, out patientSer, out courseSer, out planSetupSer);
                AriaInterface.Disconnect();
            }
            catch (Exception exception)
            {
                System.Windows.Forms.MessageBox.Show(exception.Message, "ARIA Interface Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
        }

        public void Analyze()
        {
            try
            {
                AriaInterface.Connect();
                U();
                I();
                D();
                P();                
                V();
                M();
                B();
                S();
                X();

                foreach (ChecklistItem checklistItem in checklistItems)
                    if (string.Compare(checklistItem.AutoCheckStatus, AutoCheckStatus.PASS.ToString()) == 0)
                        checklistItem.Status = true;

                long checklistSer = databaseManager.SaveResult(userId, patient.Id, patient.FirstName, patient.LastName, patientSer, course.Id, courseSer, planSetup.Id, planSetupSer, checklistItems.ToArray());

                Viewer.Start(checklistSer);
            }
            catch (Exception exception)
            {
                ShowError(exception);
            }

            try { AriaInterface.Disconnect(); }
            catch { }
        }

        private int GetNumberOfTreatmentBeams()
        {
            int numberOfTreatmentBeams = 0;
            foreach (Beam beam in planSetup.Beams)
                if (!beam.IsSetupField)
                    numberOfTreatmentBeams++;
            return numberOfTreatmentBeams;
        }

        private int GetNumberOfBeams()
        {
            return planSetup.Beams.Count();
        }

        private AutoCheckStatus CheckResult(bool passed)
        {
            return passed ? AutoCheckStatus.PASS : AutoCheckStatus.FAIL;
        }

        private void ShowError(Exception exception)
        {
            ShowError("Error", exception);
        }

        private void ShowError(string title, Exception exception)
        {
            System.Windows.Forms.MessageBox.Show(exception.Message, title, System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
        }

    }
}
