using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Text;

namespace Munq.Redis.Client.Tests
{
    public class TestConnection : IPipeConnection
    {
        private readonly bool _ownsFactory;
        private PipeFactory _factory;
        private IPipe _input, _output;

        public TestConnection(PipeFactory factory = null)
        {
            if (factory == null)
            {
                _ownsFactory = true;
                factory = new PipeFactory();
            }
            _factory = factory;

            _input  = _factory.Create();
            _output = _factory.Create();
        }
        public IPipeReader Input => _input.Reader;

        public IPipeWriter Output => _output.Writer;

        public IPipeReader RemoteInput => _output.Reader;

        public IPipeWriter RemoteOutput => _input.Writer;

        public void Dispose()
        {
            _output.Reader.CancelPendingRead();
            _input.Reader.CancelPendingRead();
            _output.Writer.Complete();
            _input.Writer.Complete();

            if (_ownsFactory) { _factory?.Dispose(); }
            _factory = null;
        }
    }
}
