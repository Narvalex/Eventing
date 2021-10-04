using Infrastructure.Messaging.Handling;

namespace Infrastructure.EntityFramework.ReadModel
{
    public abstract class EfReadModelProjectorBase
    {
        private bool checkpointReadyForExtraction = false;
        private Checkpoint inPreparationCheckpoint = Checkpoint.Start;
        private Checkpoint checkpointForExtraction = Checkpoint.Start;

        private object syncObject = new object();

        protected void PrepareCheckpoint(Checkpoint checkpoint)
        {
            this.inPreparationCheckpoint = checkpoint;
        }

        protected long LastEventNumber => this.inPreparationCheckpoint.EventNumber;

        protected void SetCheckpointForExtraction()
        {
            lock (this.syncObject)
            {
                this.checkpointReadyForExtraction = true;
                this.checkpointForExtraction = this.inPreparationCheckpoint;
            }
        }

        protected void SetCheckpointForExtraction(Checkpoint checkpoint)
        {
            this.PrepareCheckpoint(checkpoint);
            this.SetCheckpointForExtraction();

        }

        protected void DiscardCheckpoint()
        {
            this.inPreparationCheckpoint = Checkpoint.Start;
            this.checkpointReadyForExtraction = false;
        }

        public bool TryExtractCheckpoint(out Checkpoint checkpoint)
        {
            lock (this.syncObject)
            {
                checkpoint = this.checkpointForExtraction;
                if (this.checkpointReadyForExtraction)
                {
                    this.checkpointReadyForExtraction = false;
                    return true;
                }
                else
                    return false;
            }
        }
    }
}
