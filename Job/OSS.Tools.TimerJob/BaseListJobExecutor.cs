﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OSS.Tools.TimerJob
{
 
    /// <summary>
    ///  列表循环处理任务执行
    ///  从 GetExecuteSource() 获取执行数据源，循环并通过 ExecuteItem() 执行个体任务，直到没有数据源返回
    ///       如果执行时间过长，重复触发时 当前任务还在进行中，则不做任何处理
    /// </summary>
    public abstract class BaseListJobExecutor<IType> : IJobExecutor
    {
        private readonly bool _isExecuteOnce;

        /// <summary>
        ///  列表任务执行者
        /// </summary>
        protected BaseListJobExecutor():this(false)
        {
        }
        
        /// <summary>
        ///  列表任务执行者
        /// </summary>
        /// <param name="excuteOnce">是否只获取一次数据源</param>
        protected BaseListJobExecutor(bool excuteOnce)
        {
            _isExecuteOnce = excuteOnce;
        }

        /// <summary>
        ///  运行状态
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        ///   开始任务
        /// </summary>
        public async Task StartJob(CancellationToken cancellationToken)
        {
            //  任务依然在执行中，不需要再次唤起
            if (IsRunning)
                return;

            IsRunning = true;
            var page=0;
            IList<IType> list; // 结清实体list

            await OnBegin();
            while (IsStillRunning(cancellationToken)
                   && (list =await GetExecuteSource(page++))?.Count > 0)
            {
                for (var i = 0; IsStillRunning(cancellationToken) && i < list?.Count; i++)
                {
                    await ExecuteItem(list[i], i);
                }

                if (_isExecuteOnce)
                {
                    break;
                }
            }

            await OnEnd();
            IsRunning = false;
        }

        private bool IsStillRunning(CancellationToken cancellationToken)
        {
            return IsRunning
                   && !cancellationToken.IsCancellationRequested;
        }

        /// <summary>
        ///   获取list数据源, 此方法会被循环调用
        /// </summary>
        /// <returns></returns>
        protected abstract Task<IList<IType>> GetExecuteSource(int page);

        /// <summary>
        ///  个体任务执行
        /// </summary>
        /// <param name="item">单个实体</param>
        /// <param name="index">在数据源中的索引</param>
        protected abstract Task ExecuteItem(IType item, int index);

        /// <summary>
        /// 结束任务
        /// </summary>
        public Task StopJob(CancellationToken cancellationToken)
        {
            IsRunning = false;
            return Task.CompletedTask;
        }

        public Task StopJob()
        {
            return StopJob(CancellationToken.None);
        }

        /// <summary>
        ///  此轮任务开始
        /// </summary>
        protected virtual Task OnBegin()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        ///  此轮任务结束
        /// </summary>
        protected virtual Task OnEnd()
        {
            return Task.CompletedTask;
        }
        
    }
}