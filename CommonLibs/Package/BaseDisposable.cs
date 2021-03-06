﻿using System;
//using System.Threading;

namespace CommonLibs
{
    public abstract class BaseDisposable : IDisposable
    {
        //private int _isDisposed = 0;

        /// <summary>
        /// 显示释放对象资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// 检查对象是否已被显示释放了
        /// </summary>
        //protected void CheckDisposed()
        //{
        //    if (_isDisposed == 1)
        //    {
        //        throw new Exception(string.Format("The {0} object has be disposed.", this.GetType().Name));
        //    }
        //}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            //释放非托管资源 
            if (disposing)
            {
                //Interlocked.Exchange(ref _isDisposed, 1);
                GC.SuppressFinalize(this);
            }
        }
    }
}
