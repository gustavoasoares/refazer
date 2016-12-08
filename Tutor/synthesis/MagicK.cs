namespace Tutor.synthesis
{
    public class MagicK
    {
        private readonly PythonNode _input;
        private readonly PythonNode _target;

        public MagicK(PythonNode input, PythonNode target)
        {
            _input = input;
            _target = target;
        }

        public int GetK(Pattern pattern)
        {
            var matches = pattern.Matches(_input);
            var witness = -1;
            for (var i = 0; i < matches.Count; i++)
            {
                if (matches[i].Id == _target.Id)
                {
                    witness = i;
                    break;
                }
            }
            if (witness < 0)
                return -1;
            return witness;
        }

        protected bool Equals(MagicK other)
        {
            var wildCard = new Wildcard(_target.InnerNode.GetType().Name);
            var pattern = new Pattern(wildCard, 0, new AbsolutePath(0));
            return Equals(GetK(pattern), other.GetK(pattern));
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MagicK)obj);
        }

        public override int GetHashCode()
        {
            return (_target != null ? _target.GetHashCode() : 0);
        }
    }
}