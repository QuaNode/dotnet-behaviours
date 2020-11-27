using System;

namespace QuaNode {

    public class BehaviourError : Exception {
        
        public int Code = -1;

        public BehaviourError(string message) : base(message) { }
    }
}
