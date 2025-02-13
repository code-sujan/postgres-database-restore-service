﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Windows.Forms;
using Dapper;
using Npgsql;
using postgres_database_restore_tool.Dto;
using postgres_database_restore_tool.Helper;
using postgres_database_restore_tool.Validator;
using postgres_database_restore_tool.ValueObject;

namespace postgres_database_restore_tool
{
    public partial class PgAdmin : Form
    {
        public PgAdmin()
        {
            InitializeComponent();
            AddEventHandlers();
        }

        private void StartLoading(string msg)
        {
            loadingLbl.Text = msg;
            loadingLbl.Visible = true;
            loadingBar.Visible = true;
        }

        private void EndLoading()
        {
            loadingLbl.Text = "";
            loadingLbl.Visible = false;
            loadingBar.Visible = false;
        }

        const string pwdKey = "PGPASSWORD";

        private void AddEventHandlers()
        {
            Load += OnFormLoad;
            RestoreBtn.Click += OnRestore;
            FileOpenElem.Click += OnFileOpenClick;
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            var config = GetConfig();
            PortElem.Text = config.Port.ToString();
            PasswordElm.Text = config.Password;
            var commandType = new List<string>()
            {
                "pg_restore",
                "pg_dump"
            };
            var actionTypes = new List<string>()
            {
                "Drop_and_Restore",
                "Create_and_Restore",
            };
            ActionSelectorElem.DataSource = actionTypes;
            TypeSelectorElem.DataSource = commandType;
        }

        private void OnFileOpenClick(object sender, EventArgs e)
        {
            var selected = TargetLocation.ShowDialog();
            if (selected == DialogResult.OK)
            {
                MessageBox.Show(TargetLocation.FileName, "File Selected");
                SelectedFilelbl.Text = TargetLocation.FileName;
                if (string.IsNullOrWhiteSpace(DatabaseElem.Text))
                {
                    var fileName = TargetLocation.FileName.Split(new char[] { '/', '\\' }).LastOrDefault();
                    if (fileName.Contains("_"))
                    {
                        DatabaseElem.Text = fileName.Split('_').FirstOrDefault();
                    }
                }
            }
        }

        private void OnRestore(object sender, EventArgs e)
        {
            try
            {
                StartLoading("Restoring Database");

                var connection = UserConnectionValidator.ValidateConnection(new UserConnectionVo()
                {
                    Port = PortElem.Text,
                    UserName = UserNameElm.Text,
                    Password = PasswordElm.Text,
                    DatabaseName = DatabaseElem.Text,
                    ActionTypeValue = ActionSelectorElem.SelectedValue.ToString(),
                    DatabaseBackupType = TypeSelectorElem.SelectedValue.ToString(),
                    RestoreFileLocation = TargetLocation.FileName,
                });

                WorkingStatus.Text = "Restoring...";
                var bgw = new BackgroundWorker();
                bgw.DoWork += (object _, DoWorkEventArgs args) => { CommandExecutor.ExecuteRestore(connection); };
                bgw.RunWorkerCompleted += (object _, RunWorkerCompletedEventArgs args) =>
                {
                    if (args.Error == null)
                    {
                        WorkingStatus.Text = "Completed";
                        MessageBox.Show($"Database #{DatabaseElem.Text} restored successfully");
                    }
                    else
                    {
                        var msg = (args.Error as Exception)?.Message ?? "Error during operation";
                        MessageBox.Show(msg);
                    }

                    SelectedFilelbl.Text = "";
                    WorkingStatus.Text = "";
                    EndLoading();
                };
                bgw.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                EndLoading();
                SelectedFilelbl.Text = "";
                WorkingStatus.Text = "";
                MessageBox.Show(ex.Message);
                Environment.SetEnvironmentVariable(pwdKey, "");
            }
        }

        private void Register_Btn_Click(object sender, EventArgs e)
        {
            try
            {
                StartLoading("Restoring Database");

                var connection = UserConnectionValidator.ValidateConnection(new UserConnectionVo()
                {
                    Port = PortElem.Text,
                    UserName = UserNameElm.Text,
                    Password = PasswordElm.Text,
                    DatabaseName = DatabaseElem.Text,
                    ActionTypeValue = ActionSelectorElem.SelectedValue.ToString(),
                    DatabaseBackupType = TypeSelectorElem.SelectedValue.ToString(),
                    RestoreFileLocation = TargetLocation.FileName,
                });

                WorkingStatus.Text = "Restoring and registering...";
                var bgw = new BackgroundWorker();
                bgw.DoWork += (object _, DoWorkEventArgs args) =>
                {
                    CommandExecutor.ExecuteRestore(connection);

                    var userList = GetUserList(connection);
                    if (userList.Count == 0)
                        throw new Exception("Error retrieving user from primary database");

                    CopyUserToParentDb(connection, ParentDbElem.Text, userList);
                };
                bgw.RunWorkerCompleted += (object _, RunWorkerCompletedEventArgs args) =>
                {
                    if (args.Error == null)
                    {
                        WorkingStatus.Text = "Completed";
                        MessageBox.Show($"Database #{DatabaseElem.Text} restored and registered to #{ParentDbElem.Text} successfully");
                    }
                    else
                    {
                        var msg = (args.Error as Exception)?.Message ?? "Error during operation";
                        MessageBox.Show(msg);
                    }

                    SelectedFilelbl.Text = "";
                    WorkingStatus.Text = "";
                    EndLoading();
                };
                bgw.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                EndLoading();
                SelectedFilelbl.Text = "";
                WorkingStatus.Text = "";
                MessageBox.Show(ex.Message);
                Environment.SetEnvironmentVariable(pwdKey, "");
            }
        }

        private static void CopyUserToParentDb(UserConnectionVo connection, string parentDb, List<TempUser> userList)
        {
            var userDatabaseConnectionString = $"Server=localhost; port={connection.Port}; Username={connection.UserName}; Password={connection.Password}; Database={parentDb};";
            using (var conn4 = new NpgsqlConnection(userDatabaseConnectionString))
            {
                var mainUser = conn4.QuerySingle<TempUser>("Select * from public.\"AspNetUsers\" where id = -1");
                var insertUserIntoInternalTableQuery =
                    "INSERT INTO public.\"AspNetUsers\"(user_name, normalized_user_name, email, normalized_email, email_confirmed, password_hash, security_stamp, concurrency_stamp, phone_number, phone_number_confirmed, two_factor_enabled, lockout_enabled, access_failed_count, branch_id, rec_by, rec_date, rec_status, user_language, txn_date, gmt, running_date, user_status, is_parent_user, parent_user_id, created_at, updated_at, created_by_user, updated_by_user, is_in_trial, business_name, primary_contact_number, mobile_number, country, state, city, first_name, middle_name, last_name, associated_with_all_branches, user_level)" +
                    "VALUES (@user_name, @normalized_user_name, @email, @normalized_email, @email_confirmed, @password_hash, @security_stamp, @concurrency_stamp, @phone_number, @phone_number_confirmed, @two_factor_enabled, @lockout_enabled, @access_failed_count, @branch_id, @rec_by, @rec_date, @rec_status, @user_language, @txn_date, @gmt, @running_date, @user_status, @is_parent_user, @parent_user_id, @created_at, @updated_at, @created_by_user, @updated_by_user, @is_in_trial, @business_name, @primary_contact_number, @mobile_number, @country, @state, @city, @first_name, @middle_name, @last_name, false, @user_level)";

                foreach (var tempUser in userList)
                {
                    tempUser.password_hash = mainUser.password_hash;
                    tempUser.security_stamp = mainUser.security_stamp;
                    if (conn4.ExecuteScalar<bool>("select count(1) from public.\"AspNetUsers\" where email = @email", new { email = tempUser.email }))
                        continue;
                    tempUser.branch_id = 1;
                    conn4.Execute(insertUserIntoInternalTableQuery, tempUser);
                }

                var currentParentUserId = userList.Where(x => x.parent_user_id != null).Select(x => x.parent_user_id).FirstOrDefault();
                if (currentParentUserId != null)
                {
                    var parentUser = conn4.QuerySingle("Select * from public.\"AspNetUsers\" where email = @email", new { email = connection.DatabaseName });
                    conn4.Execute("UPDATE public.\"AspNetUsers\" SET parent_user_id = @real_id WHERE parent_user_id = @current_id", new { real_id = parentUser.id, current_id = currentParentUserId});   
                }
            }
        }

        private static AppSettingConfig GetConfig()
        {
            return JsonSerializer.Deserialize<AppSettingConfig>(File.ReadAllText("appsettings.json"), new JsonSerializerOptions{PropertyNameCaseInsensitive = true});
        }

        private static List<TempUser> GetUserList(UserConnectionVo connection)
        {
            var connectionString = $"Server=localhost; port={connection.Port}; Username={connection.UserName}; Password={connection.Password}; Database={connection.DatabaseName};";
            List<TempUser> userList;
            using (var parentConnection = new NpgsqlConnection(connectionString))
            {
                var getUserQuery = "select * from public.\"AspNetUsers\" ";
                userList = parentConnection.Query<TempUser>(getUserQuery).ToList();
            }

            return userList;
        }
    }

    internal class AppSettingConfig
    {
        public long Port { get; set; }
        public string Password { get; set; }
    }
}