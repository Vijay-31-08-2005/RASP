namespace Rasp {
    public interface ICommand {
        public string Usage {get;}
        public abstract void Execute( string[] args);

        public abstract void Info();
    }

}
