﻿using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Threading;

namespace TandaSpreadsheetTool
{
    public partial class Form1 : Form
    {
        Networker networker;
        RosterBuilder builder;
        Thread bgThread;
        List<FormattedRoster> rosters;

      

        public Form1()
        {
            InitializeComponent();

            
            networker = new Networker();
            rosters = new List<FormattedRoster>();
            networker.LoadUsername();

            if(networker.LastUser != "")
            {
                txtBxUName.Text = networker.LastUser;
            }

            txtBxDateFrom.Text = DateTime.Now.AddDays(-7).ToShortDateString();
            txtBxDateTo.Text = DateTime.Now.ToShortDateString();

            
           
        }

        void ShowMainPanel()
        {
            pnlLogIn.Visible = false;
            pnlMain.Visible = true;
        }

        void ShowLogInPanel()
        {
            pnlMain.Visible = false;
            pnlLogIn.Visible = true;
        }

        void EnableLogInPnl()
        {
            pnlLogIn.Enabled = true;
            txtBxPwd.Text = "";
        }
        void LoggedIn()
        {
            pnlLogIn.Visible = false;
            txtBxPwd.Text = "";
            pnlMain.Visible = true;
        }
        void SetUserNameToOld()
        {
            txtBxUName.Text = networker.LastUser;
            txtBxPwd.Text = "";
        }

        async  void LogIn()
        {
            
            if (networker.LastUser != "")
            {
                if (object.Equals(txtBxUName.Text, networker.LastUser))
                {
                    if (networker.SignIn(txtBxPwd.Text))
                    {
                        Invoke(new MethodInvoker(LoggedIn));
                        builder = new RosterBuilder(networker, this);
                      
                    }
                    else
                    {
                        MessageBox.Show("Failed to Authenticate", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                        Invoke(new MethodInvoker(EnableLogInPnl));
                    }

                }
                else
                {
                    var dResult = MessageBox.Show("The username is different from the stored username, only one user may be signed in. Sign previous user out?"
                        , "Sign out Old User?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                    if (dResult == DialogResult.Yes)
                    {

                     
                        networker.ClearFileData();
                        

                      var gotToken = await  networker.GetToken(txtBxUName.Text, txtBxPwd.Text);

                        if (!gotToken)
                        {
                            Invoke(new MethodInvoker(EnableLogInPnl));
                            MessageBox.Show("Log In failed: " + networker.LastNetErrMsg, "Log In Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        else
                        {
                            builder = new RosterBuilder(networker, this);
                            Invoke(new MethodInvoker(LoggedIn));
                        }
                    }
                    else
                    {
                        Invoke(new MethodInvoker(EnableLogInPnl));
                        Invoke(new MethodInvoker(SetUserNameToOld));

                    }
                }
            }
            else
            {
                var gotToken = await networker.GetToken(txtBxUName.Text, txtBxPwd.Text);

                if (!gotToken)
                {
                    Invoke(new MethodInvoker(EnableLogInPnl));
                    MessageBox.Show("Log In failed: " + networker.LastNetErrMsg, "Log In Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    builder = new RosterBuilder(networker, this);
                    Invoke(new MethodInvoker(LoggedIn));
                }


            }

          

           
        }

       async void MakeRoster()
        {
            var dateFrom = txtBxDateFrom.Text;
            var dateTo = txtBxDateTo.Text;

            if (dateFrom.Length != 8 & dateFrom.Length != 10)
            {
                MessageBox.Show("Invalid date use DD-MM-YY format", "Invalid Date", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (dateTo.Length != 8 & dateTo.Length != 10)
            {
                MessageBox.Show("Invalid date use DD-MM-YY format", "Invalid Date", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            
          FormattedRoster newRoster= await builder.BuildRoster(dateFrom, dateTo);

            rosters.Add(newRoster);
            MessageBox.Show("Added roster");
            btnSaveJSON.Enabled = true;
        }

        public void FormattingComplete()
        {
           if (ckBxOpenFolder.Checked)
            {
                Process.Start(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TandaJson"));
            }
            else
            {
                MessageBox.Show("Saved File to Documents/TandaJson");
            }
                
            
        }

        private void btnLogIn_Click(object sender, EventArgs e)
        {
         
            bgThread = new Thread(() => LogIn());
            pnlLogIn.Enabled = false;
            bgThread.Start();
        }

        private void btnTest_Click(object sender, EventArgs e)
        {
           

            if (builder!=null)
            {
               
                    btnSaveJSON.Enabled = false;
                    bgThread = new Thread(new ThreadStart(MakeRoster));
                    bgThread.Start();
                
            }
            else
            {
                builder = new RosterBuilder(networker, this);
            }
            

            

        }

        
    }
}
