﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DataAccess.Entity;
using DataAccess.BLL;
using ServiceHost.Common;
using ScheduleJobDesktop.Common;

namespace ScheduleJobDesktop.UI.ManageScheduleJob
{
    /// <summary>
    /// 任务管理添加界面
    /// </summary>
    public partial class Create : UserControl
    {
        JobDetail jobDetail;
        static Create instance;
        
        /// <summary>
        /// 返回一个该控件的实例。如果之前该控件已经被创建，直接返回已创建的控件。
        /// 此处采用单键模式对控件实例进行缓存，避免因界面切换重复创建和销毁对象。
        /// </summary>
        public static Create Instance {
            get {
                if (instance == null)
                {
                    instance = new Create();
                }
                instance.jobDetail = new JobDetail(); // 创建新的关联对象，可以在“数据实体层”中指定对象的默认值。
                instance.BindObjectToForm(); // 每次返回该控件的实例前，都将关联对象的默认值，绑定至界面控件进行显示。
                return instance;
            }
        }
        
        public static Create BindJobDetail(JobDetail job)
        {
            if (instance == null)
            {
                instance = new Create();
            }
            instance.jobDetail = job; // 创建新的关联对象，可以在“数据实体层”中指定对象的默认值。
            instance.BindObjectToForm(); // 每次返回该控件的实例前，都将关联对象的默认值，绑定至界面控件进行显示。
            return instance;
        }

        /// <summary>
        /// 私有的控件实例化方法，创建实例只能通过该控件的Instance属性实现。
        /// </summary>
        private Create()
        {
            InitializeComponent();
            List<KeyValuePair<string, string>> listFreq = new List<KeyValuePair<string, string>>();
            listFreq.Add(new KeyValuePair<string, string>("循环执行", "1"));
            listFreq.Add(new KeyValuePair<string, string>("只执行一次", "2"));
            ComBoxFreq.DisplayMember = "Key";
            ComBoxFreq.ValueMember = "Value";
            ComBoxFreq.DataSource = listFreq;

            this.Dock = DockStyle.Fill;
        }

        /// <summary>
        /// 用户单击“保存”按钮时的事件处理方法。
        /// </summary>
        private void BtnSave_Click(object sender, EventArgs e)
        {
            BindFormlToObject(); // 进行数据绑定
            if (jobDetail.JobId>0)
            {
                JobDetailBLL.CreateInstance().Update(jobDetail); // 调用“业务逻辑层”的方法，检查有效性后更新至数据库。
            }
            else
            {
                JobDetailBLL.CreateInstance().Insert(jobDetail); // 调用“业务逻辑层”的方法，检查有效性后插入至数据库。
            }
            FormSysMessage.ShowSuccessMsg("保存成功，单击“确定”按钮返回。");
            //Default.GotoLastPage();                    // 将该模块的信息列表的页码转至最后一页。
            FormMain.LoadNewControl(Default.Instance); // 载入该模块的信息列表界面至主窗体显示。
        }

        /// <summary>
        /// 用户单击“取消”按钮时的事件处理方法。
        /// </summary>
        private void BtnCancel_Click(object sender, EventArgs e)
        {
            FormMain.LoadNewControl(Default.Instance); // 载入该模块的信息列表界面至主窗体显示。
        }

        #region 界面控件与关联对象之间的绑定方法

        /// <summary>
        /// 将界面控件中的值，绑定给关联对象。
        /// </summary>
        private void BindFormlToObject()
        {
            jobDetail.JobChineseName = DataValid.GetNullOrString(TxtScheduleChineseName.Text);
            jobDetail.JobName = DataValid.GetNullOrString(TxtJobIdentity.Text);
            jobDetail.JobServiceURL = DataValid.GetNullOrString(TxtServiceAddress.Text);
            jobDetail.State = GetJobState();

            if (ComBoxFreq.SelectedValue==null||string.IsNullOrEmpty(ComBoxFreq.SelectedValue.ToString()))
            {
                throw new CustomException("请选择执行频率！", ExceptionType.Warn);
            }
            jobDetail.ExecutedFreq = Convert.ToByte(ComBoxFreq.SelectedValue);
            jobDetail.StartDate = DateTimePickerStart.Value;
            jobDetail.EndDate = DateTimePickerEnd.Value;
            jobDetail.IntervalType = GetInterval().Item1;
            jobDetail.Interval = GetInterval().Item2;

            if (string.IsNullOrEmpty(TxtRecordNum.Text))
            {
                throw new CustomException("请输入每次处理的数据条数！", ExceptionType.Warn);
            }
            jobDetail.PageSize = DataValid.GetNullOrInt(TxtRecordNum.Text).Value;

            jobDetail.Description = DataValid.GetNullOrString(TxtNoteDescription.Text);  // 备注说明
        }

        /// <summary>
        /// 将关联对象中的值，绑定至界面控件进行显示。
        /// </summary>
        internal void BindObjectToForm()
        {
            TxtScheduleChineseName.Text = jobDetail.JobChineseName;
            TxtJobIdentity.Text = jobDetail.JobName;
            TxtServiceAddress.Text = jobDetail.JobServiceURL;
            SetJobState(jobDetail.State);
            ComBoxFreq.SelectedValue = jobDetail.ExecutedFreq.ToString();
            if (jobDetail.StartDate > DateTime.MinValue)
            {
                DateTimePickerStart.Value = jobDetail.StartDate;
            }
            if (jobDetail.EndDate > DateTime.MinValue)
            {
                DateTimePickerEnd.Value = jobDetail.EndDate;
            }
            SetInterval(jobDetail.IntervalType, jobDetail.Interval);

            TxtRecordNum.Text = jobDetail.PageSize.ToString();
            TxtNoteDescription.Text = jobDetail.Description;  // 备注说明
        }

        Tuple<byte, int> GetInterval()
        {
            var radioButtons = PnlInterval.Controls.OfType<RadioButton>();
            if (radioButtons.Any(t => t.Checked))
            {
                var selected = radioButtons.First(t => t.Checked);
                switch (selected.Name)
                {
                    case "RadioBtnDay":
                        if (string.IsNullOrEmpty(TxtDay.Text))
                        {
                            break;
                        }
                        return Tuple.Create<byte, int>((byte)IntervalType.Day, DataValid.GetNullOrInt(TxtDay.Text).Value);
                    case "RadioBtnHour":
                        if (string.IsNullOrEmpty(TxtHour.Text))
                        {
                            break;
                        }
                        return Tuple.Create<byte, int>((byte)IntervalType.Hour, DataValid.GetNullOrInt(TxtHour.Text).Value);
                    case "RadioBtnMinute":
                        if (string.IsNullOrEmpty(TxtMinute.Text))
                        {
                            break;
                        }
                        return Tuple.Create<byte, int>((byte)IntervalType.Minute, DataValid.GetNullOrInt(TxtMinute.Text).Value);
                    default:
                        break;
                }
            }
            throw new CustomException("请选择一个任务间隔类型并填写执行间隔！", ExceptionType.Warn);
        }

        byte GetJobState()
        {
            var radioButtons = PnlJobState.Controls.OfType<RadioButton>();
            if (radioButtons.Any(t => t.Checked))
            {
                var selected = radioButtons.First(t => t.Checked);
                switch (selected.Name)
                {
                    case "RadioBtnWaiting": return (byte)JobState.Waiting;
                    case "RadioBtnRunning": return (byte)JobState.Running;
                    case "RadioBtnStopping": return (byte)JobState.Stopping;
                    default:
                        break;
                }
            }
            throw new CustomException("请选择一个任务状态！", ExceptionType.Warn);
        }

        void SetInterval(byte intervalType, int interval)
        {
            switch ((IntervalType)intervalType)
            {
                case IntervalType.Day:
                    RadioBtnDay.Checked = true;
                    TxtDay.Text = interval.ToString();
                    break;
                case IntervalType.Hour:
                    RadioBtnHour.Checked = true;
                    TxtHour.Text = interval.ToString();
                    break;
                case IntervalType.Minute:
                    RadioBtnMinute.Checked = true;
                    TxtMinute.Text = interval.ToString();
                    break;
                default:
                    break;
            }
        }

        void SetJobState(byte jobState)
        {
            switch ((JobState)jobState)
            {
                case JobState.Waiting:
                    RadioBtnWaiting.Checked = true;
                    break;
                case JobState.Running:
                    RadioBtnRunning.Checked = true;
                    break;
                case JobState.Stopping:
                    RadioBtnStopping.Checked = true;
                    break;
                default:
                    break;
            }
        }
        #endregion 界面控件与关联对象之间的绑定方法
    }
}
