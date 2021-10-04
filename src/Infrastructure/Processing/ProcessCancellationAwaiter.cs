using System;
using System.Threading;

namespace Infrastructure.Processing
{
    /// <summary>
    /// Este componente mantiene un handle de espera por si alguien cambia el archivo de 
    /// configuracion
    /// </summary>
    public class ProcessCancellationAwaiter : IDisposable
    {
        private readonly WaitHandle waitHandle;
        private readonly CancellationTokenSource cancellationTokenSource;

        public ProcessCancellationAwaiter(WaitHandle waitHandle)
        {
            this.waitHandle = waitHandle;
            this.cancellationTokenSource = new CancellationTokenSource();
        }

        public void WaitCancellation()
        {
            this.waitHandle.WaitOne();
            this.cancellationTokenSource.Cancel();
        }

        public CancellationToken Token => this.cancellationTokenSource.Token;

        public void Dispose()
        {
            using (this.cancellationTokenSource)
            {
                if (!this.cancellationTokenSource.IsCancellationRequested)
                    this.cancellationTokenSource.Cancel();
            }
        }
    }
}
