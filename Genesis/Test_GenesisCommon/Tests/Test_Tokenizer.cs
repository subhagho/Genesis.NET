using System;
using System.Collections.Generic;
using Xunit;
using LibZConfig.Common.Utils;
using LibGenesisCommon.Query;

namespace LibGenesisCommon.Tests
{
    public class Test_Tokenizer
    {
        [Fact]
        public void BasicQuery()
        {
            try
            {
                string query = "(column1 = \"This is a quoted \\\"string\\\"\" && column2 != 1293807.00 && (column3.name={\'abc\', \'def\', \'xyz\'} || column4.date!=[\'1973-07-01\', \'2007-07-01\']))";
                LogUtils.Info(String.Format("Tokenizing Query:[{0}]", query));

                Tokenizer tokenizer = new Tokenizer();
                List<Token> tokens = tokenizer.Tokenize(query);
                Assert.NotNull(tokens);
                Assert.NotEmpty(tokens);
                LogUtils.Info("****************************BEING TOKENS****************************");
                foreach(Token tk in tokens)
                {
                    LogUtils.Info(tk.ToString());
                }
                LogUtils.Info("****************************END TOKENS****************************");
            }
            catch (Exception e)
            {
                LogUtils.Error(e);
                throw e;
            }
        }

        [Fact]
        public void QueryWithMath()
        {
            try
            {
                string query = "(column1 = \"This is a quoted \\\"string\\\"\" && column2 != column_val + 1293807.00/column_val - 8888 && (column3.name={\'abc\', \'def\', \'xyz\'} || column4.date!=[\'1973-07-01\', \'2007-07-01\']))";
                LogUtils.Info(String.Format("Tokenizing Query:[{0}]", query));

                Tokenizer tokenizer = new Tokenizer();
                List<Token> tokens = tokenizer.Tokenize(query);
                Assert.NotNull(tokens);
                Assert.NotEmpty(tokens);
                LogUtils.Info("****************************BEING TOKENS****************************");
                foreach (Token tk in tokens)
                {
                    LogUtils.Info(tk.ToString());
                }
                LogUtils.Info("****************************END TOKENS****************************");
            }
            catch (Exception e)
            {
                LogUtils.Error(e);
                throw e;
            }
        }
    }
}
