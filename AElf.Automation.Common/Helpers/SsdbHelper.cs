using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace AElf.Automation.Common.Helpers
{
    public class SsdbHelper
    {
        public SsdbClient Client { get; set; }

        public SsdbHelper(string host, int port = 8888)
        {
            Client = new SsdbClient(host, port);
        }
    }

    public class SsdbClient
    {
        private readonly Link _link;
        private string _respCode;

        public SsdbClient(string host, int port)
        {
            _link = new Link(host, port);
        }

        ~SsdbClient()
        {
            this.Close();
        }

        public void Close()
        {
            _link.Close();
        }

        public List<byte[]> Request(string cmd, params string[] args)
        {
            return _link.Request(cmd, args);
        }

        public List<byte[]> Request(string cmd, params byte[][] args)
        {
            return _link.Request(cmd, args);
        }

        public List<byte[]> Request(List<byte[]> req)
        {
            return _link.Request(req);
        }


        private void Assert_ok()
        {
            if (_respCode != "ok")
            {
                throw new Exception(_respCode);
            }
        }

        private byte[] _bytes(string s)
        {
            return Encoding.Default.GetBytes(s);
        }

        private string _string(byte[] bs)
        {
            return Encoding.Default.GetString(bs);
        }

        private KeyValuePair<string, byte[]>[] Parse_scan_resp(List<byte[]> resp)
        {
            _respCode = _string(resp[0]);
            this.Assert_ok();

            int size = (resp.Count - 1) / 2;
            KeyValuePair<string, byte[]>[] kvs = new KeyValuePair<string, byte[]>[size];
            for (int i = 0; i < size; i += 1)
            {
                string key = _string(resp[i * 2 + 1]);
                byte[] val = resp[i * 2 + 2];
                kvs[i] = new KeyValuePair<string, byte[]>(key, val);
            }
            return kvs;
        }

        /***** kv *****/

        public bool Exists(byte[] key)
        {
            List<byte[]> resp = Request("exists", key);
            _respCode = _string(resp[0]);
            if (_respCode == "not_found")
            {
                return false;
            }
            this.Assert_ok();
            if (resp.Count != 2)
            {
                throw new Exception("Bad response!");
            }
            return (_string(resp[1]) == "1" ? true : false);
        }

        public bool Exists(string key)
        {
            return this.Exists(_bytes(key));
        }

        public void Set(byte[] key, byte[] val)
        {
            List<byte[]> resp = Request("set", key, val);
            _respCode = _string(resp[0]);
            this.Assert_ok();
        }

        public void Set(string key, string val)
        {
            this.Set(_bytes(key), _bytes(val));
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <returns>returns true if name.key is found, otherwise returns false.</returns>
        public bool get(byte[] key, out byte[] val)
        {
            val = null;
            List<byte[]> resp = Request("get", key);
            _respCode = _string(resp[0]);
            if (_respCode == "not_found")
            {
                return false;
            }
            this.Assert_ok();
            if (resp.Count != 2)
            {
                throw new Exception("Bad response!");
            }
            val = resp[1];
            return true;
        }

        public bool Get(string key, out byte[] val)
        {
            return this.get(_bytes(key), out val);
        }

        public bool Get(string key, out string val)
        {
            val = null;
            byte[] bs;
            if (!this.Get(key, out bs))
            {
                return false;
            }
            val = _string(bs);
            return true;
        }

        public void del(byte[] key)
        {
            List<byte[]> resp = Request("del", key);
            _respCode = _string(resp[0]);
            this.Assert_ok();
        }

        public void del(string key)
        {
            this.del(_bytes(key));
        }

        public KeyValuePair<string, byte[]>[] scan(string key_start, string key_end, Int64 limit)
        {
            List<byte[]> resp = Request("scan", key_start, key_end, limit.ToString());
            return Parse_scan_resp(resp);
        }

        public KeyValuePair<string, byte[]>[] rscan(string key_start, string key_end, Int64 limit)
        {
            List<byte[]> resp = Request("rscan", key_start, key_end, limit.ToString());
            return Parse_scan_resp(resp);
        }

        /***** hash *****/

        public void hset(byte[] name, byte[] key, byte[] val)
        {
            List<byte[]> resp = Request("hset", name, key, val);
            _respCode = _string(resp[0]);
            this.Assert_ok();
        }

        public void hset(string name, string key, byte[] val)
        {
            this.hset(_bytes(name), _bytes(key), val);
        }

        public void hset(string name, string key, string val)
        {
            this.hset(_bytes(name), _bytes(key), _bytes(val));
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="name"></param>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <returns>returns true if name.key is found, otherwise returns false.</returns>
        public bool hget(byte[] name, byte[] key, out byte[] val)
        {
            val = null;
            List<byte[]> resp = Request("hget", name, key);
            _respCode = _string(resp[0]);
            if (_respCode == "not_found")
            {
                return false;
            }
            this.Assert_ok();
            if (resp.Count != 2)
            {
                throw new Exception("Bad response!");
            }
            val = resp[1];
            return true;
        }

        public bool Hget(string name, string key, out byte[] val)
        {
            return this.hget(_bytes(name), _bytes(key), out val);
        }

        public bool Hget(string name, string key, out string val)
        {
            val = null;
            byte[] bs;
            if (!this.Hget(name, key, out bs))
            {
                return false;
            }
            val = _string(bs);
            return true;
        }

        public void Hdel(byte[] name, byte[] key)
        {
            List<byte[]> resp = Request("hdel", name, key);
            _respCode = _string(resp[0]);
            this.Assert_ok();
        }

        public void Hdel(string name, string key)
        {
            this.Hdel(_bytes(name), _bytes(key));
        }

        public bool hexists(byte[] name, byte[] key)
        {
            List<byte[]> resp = Request("hexists", name, key);
            _respCode = _string(resp[0]);
            if (_respCode == "not_found")
            {
                return false;
            }
            this.Assert_ok();
            if (resp.Count != 2)
            {
                throw new Exception("Bad response!");
            }
            return (_string(resp[1]) == "1" ? true : false);
        }

        public bool hexists(string name, string key)
        {
            return this.hexists(_bytes(name), _bytes(key));
        }

        public Int64 hsize(byte[] name)
        {
            List<byte[]> resp = Request("hsize", name);
            _respCode = _string(resp[0]);
            this.Assert_ok();
            if (resp.Count != 2)
            {
                throw new Exception("Bad response!");
            }
            return Int64.Parse(_string(resp[1]));
        }

        public Int64 hsize(string name)
        {
            return this.hsize(_bytes(name));
        }

        public KeyValuePair<string, byte[]>[] hscan(string name, string key_start, string key_end, Int64 limit)
        {
            List<byte[]> resp = Request("hscan", name, key_start, key_end, limit.ToString());
            return Parse_scan_resp(resp);
        }

        public KeyValuePair<string, byte[]>[] hrscan(string name, string key_start, string key_end, Int64 limit)
        {
            List<byte[]> resp = Request("hrscan", name, key_start, key_end, limit.ToString());
            return Parse_scan_resp(resp);
        }

        public void multi_hset(byte[] name, KeyValuePair<byte[], byte[]>[] kvs)
        {
            byte[][] req = new byte[(kvs.Length * 2) + 1][];
            req[0] = name;
            for (int i = 0; i < kvs.Length; i++)
            {
                req[(2 * i) + 1] = kvs[i].Key;
                req[(2 * i) + 2] = kvs[i].Value;

            }
            List<byte[]> resp = Request("multi_hset", req);
            _respCode = _string(resp[0]);
            this.Assert_ok();
        }

        public void multi_hset(string name, KeyValuePair<string, string>[] kvs)
        {
            KeyValuePair<byte[], byte[]>[] req = new KeyValuePair<byte[], byte[]>[kvs.Length];
            for (int i = 0; i < kvs.Length; i++)
            {
                req[i] = new KeyValuePair<byte[], byte[]>(_bytes(kvs[i].Key), _bytes(kvs[i].Value));
            }
            this.multi_hset(_bytes(name), req);
        }

        public void multi_hdel(byte[] name, byte[][] keys)
        {
            byte[][] req = new byte[keys.Length + 1][];
            req[0] = name;
            for (int i = 0; i < keys.Length; i++)
            {
                req[i + 1] = keys[i];
            }
            List<byte[]> resp = Request("multi_hdel", req);
            _respCode = _string(resp[0]);
            this.Assert_ok();
        }

        public void multi_hdel(string name, string[] keys)
        {
            byte[][] req = new byte[keys.Length][];
            for (int i = 0; i < keys.Length; i++)
            {
                req[i] = _bytes(keys[i]);
            }
            this.multi_hdel(_bytes(name), req);
        }

        public KeyValuePair<string, byte[]>[] multi_hget(byte[] name, byte[][] keys)
        {
            byte[][] req = new byte[keys.Length + 1][];
            req[0] = name;
            for (int i = 0; i < keys.Length; i++)
            {
                req[i + 1] = keys[i];
            }
            List<byte[]> resp = Request("multi_hget", req);
            KeyValuePair<string, byte[]>[] ret = Parse_scan_resp(resp);

            return ret;
        }

        public KeyValuePair<string, byte[]>[] multi_hget(string name, string[] keys)
        {
            byte[][] req = new byte[keys.Length][];
            for (int i = 0; i < keys.Length; i++)
            {
                req[i] = _bytes(keys[i]);
            }
            return this.multi_hget(_bytes(name), req);
        }

        /***** zset *****/

        public void Zset(byte[] name, byte[] key, Int64 score)
        {
            List<byte[]> resp = Request("zset", name, key, _bytes(score.ToString()));
            _respCode = _string(resp[0]);
            this.Assert_ok();
        }

        public void zset(string name, string key, Int64 score)
        {
            this.Zset(_bytes(name), _bytes(key), score);
        }

        public Int64 Zincr(byte[] name, byte[] key, Int64 increment)
        {
            List<byte[]> resp = Request("zincr", name, key, _bytes(increment.ToString()));
            _respCode = _string(resp[0]);
            this.Assert_ok();
            if (resp.Count != 2)
            {
                throw new Exception("Bad response!");
            }
            return Int64.Parse(_string(resp[1]));
        }

        public Int64 Zincr(string name, string key, Int64 increment)
        {
            return this.Zincr(_bytes(name), _bytes(key), increment);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="name"></param>
        /// <param name="key"></param>
        /// <param name="score"></param>
        /// <returns>returns true if name.key is found, otherwise returns false.</returns>
        public bool Zget(byte[] name, byte[] key, out Int64 score)
        {
            score = -1;
            List<byte[]> resp = Request("zget", name, key);
            _respCode = _string(resp[0]);
            if (_respCode == "not_found")
            {
                return false;
            }
            this.Assert_ok();
            if (resp.Count != 2)
            {
                throw new Exception("Bad response!");
            }
            score = Int64.Parse(_string(resp[1]));
            return true;
        }

        public bool Zget(string name, string key, out Int64 score)
        {
            return this.Zget(_bytes(name), _bytes(key), out score);
        }

        public void Zdel(byte[] name, byte[] key)
        {
            List<byte[]> resp = Request("zdel", name, key);
            _respCode = _string(resp[0]);
            this.Assert_ok();
        }

        public void Zdel(string name, string key)
        {
            this.Zdel(_bytes(name), _bytes(key));
        }

        public Int64 Zsize(byte[] name)
        {
            List<byte[]> resp = Request("zsize", name);
            _respCode = _string(resp[0]);
            this.Assert_ok();
            if (resp.Count != 2)
            {
                throw new Exception("Bad response!");
            }
            return Int64.Parse(_string(resp[1]));
        }

        public Int64 Zsize(string name)
        {
            return this.Zsize(_bytes(name));
        }

        public bool Zexists(byte[] name, byte[] key)
        {
            List<byte[]> resp = Request("zexists", name, key);
            _respCode = _string(resp[0]);
            if (_respCode == "not_found")
            {
                return false;
            }
            this.Assert_ok();
            if (resp.Count != 2)
            {
                throw new Exception("Bad response!");
            }
            return (_string(resp[1]) == "1" ? true : false);
        }

        public bool Zexists(string name, string key)
        {
            return this.Zexists(_bytes(name), _bytes(key));
        }

        public KeyValuePair<string, Int64>[] Zrange(string name, Int32 offset, Int32 limit)
        {
            List<byte[]> resp = Request("zrange", name, offset.ToString(), limit.ToString());
            KeyValuePair<string, byte[]>[] kvs = Parse_scan_resp(resp);
            KeyValuePair<string, Int64>[] ret = new KeyValuePair<string, Int64>[kvs.Length];
            for (int i = 0; i < kvs.Length; i++)
            {
                string key = kvs[i].Key;
                Int64 score = Int64.Parse(_string(kvs[i].Value));
                ret[i] = new KeyValuePair<string, Int64>(key, score);
            }
            return ret;
        }

        public KeyValuePair<string, Int64>[] Zrrange(string name, Int32 offset, Int32 limit)
        {
            List<byte[]> resp = Request("zrrange", name, offset.ToString(), limit.ToString());
            KeyValuePair<string, byte[]>[] kvs = Parse_scan_resp(resp);
            KeyValuePair<string, Int64>[] ret = new KeyValuePair<string, Int64>[kvs.Length];
            for (int i = 0; i < kvs.Length; i++)
            {
                string key = kvs[i].Key;
                Int64 score = Int64.Parse(_string(kvs[i].Value));
                ret[i] = new KeyValuePair<string, Int64>(key, score);
            }
            return ret;
        }

        public KeyValuePair<string, Int64>[] Zscan(string name, string key_start, Int64 score_start, Int64 score_end, Int64 limit)
        {
            string score_s = "";
            string score_e = "";
            if (score_start != Int64.MinValue)
            {
                score_s = score_start.ToString();
            }
            if (score_end != Int64.MaxValue)
            {
                score_e = score_end.ToString();
            }
            List<byte[]> resp = Request("zscan", name, key_start, score_s, score_e, limit.ToString());
            KeyValuePair<string, byte[]>[] kvs = Parse_scan_resp(resp);
            KeyValuePair<string, Int64>[] ret = new KeyValuePair<string, Int64>[kvs.Length];
            for (int i = 0; i < kvs.Length; i++)
            {
                string key = kvs[i].Key;
                Int64 score = Int64.Parse(_string(kvs[i].Value));
                ret[i] = new KeyValuePair<string, Int64>(key, score);
            }
            return ret;
        }

        public KeyValuePair<string, Int64>[] Zrscan(string name, string key_start, Int64 score_start, Int64 score_end, Int64 limit)
        {
            string score_s = "";
            string score_e = "";
            if (score_start != Int64.MaxValue)
            {
                score_s = score_start.ToString();
            }
            if (score_end != Int64.MinValue)
            {
                score_e = score_end.ToString();
            }
            List<byte[]> resp = Request("zrscan", name, key_start, score_s, score_e, limit.ToString());
            KeyValuePair<string, byte[]>[] kvs = Parse_scan_resp(resp);
            KeyValuePair<string, Int64>[] ret = new KeyValuePair<string, Int64>[kvs.Length];
            for (int i = 0; i < kvs.Length; i++)
            {
                string key = kvs[i].Key;
                Int64 score = Int64.Parse(_string(kvs[i].Value));
                ret[i] = new KeyValuePair<string, Int64>(key, score);
            }
            return ret;
        }

        public void Multi_zset(byte[] name, KeyValuePair<byte[], Int64>[] kvs)
        {
            byte[][] req = new byte[(kvs.Length * 2) + 1][];
            req[0] = name;
            for (int i = 0; i < kvs.Length; i++)
            {
                req[(2 * i) + 1] = kvs[i].Key;
                req[(2 * i) + 2] = _bytes(kvs[i].Value.ToString());

            }
            List<byte[]> resp = Request("multi_zset", req);
            _respCode = _string(resp[0]);
            this.Assert_ok();
        }

        public void Multi_zset(string name, KeyValuePair<string, Int64>[] kvs)
        {
            KeyValuePair<byte[], Int64>[] req = new KeyValuePair<byte[], Int64>[kvs.Length];
            for (int i = 0; i < kvs.Length; i++)
            {
                req[i] = new KeyValuePair<byte[], Int64>(_bytes(kvs[i].Key), kvs[i].Value);
            }
            this.Multi_zset(_bytes(name), req);
        }

        public void Multi_zdel(byte[] name, byte[][] keys)
        {
            byte[][] req = new byte[keys.Length + 1][];
            req[0] = name;
            for (int i = 0; i < keys.Length; i++)
            {
                req[i + 1] = keys[i];
            }
            List<byte[]> resp = Request("multi_zdel", req);
            _respCode = _string(resp[0]);
            this.Assert_ok();
        }

        public void Multi_zdel(string name, string[] keys)
        {
            byte[][] req = new byte[keys.Length][];
            for (int i = 0; i < keys.Length; i++)
            {
                req[i] = _bytes(keys[i]);
            }
            this.Multi_zdel(_bytes(name), req);
        }

        public KeyValuePair<string, Int64>[] Multi_zget(byte[] name, byte[][] keys)
        {
            byte[][] req = new byte[keys.Length + 1][];
            req[0] = name;
            for (int i = 0; i < keys.Length; i++)
            {
                req[i + 1] = keys[i];
            }
            List<byte[]> resp = Request("multi_zget", req);
            KeyValuePair<string, byte[]>[] kvs = Parse_scan_resp(resp);
            KeyValuePair<string, Int64>[] ret = new KeyValuePair<string, Int64>[kvs.Length];
            for (int i = 0; i < kvs.Length; i++)
            {
                string key = kvs[i].Key;
                Int64 score = Int64.Parse(_string(kvs[i].Value));
                ret[i] = new KeyValuePair<string, Int64>(key, score);
            }
            return ret;
        }

        public KeyValuePair<string, Int64>[] Multi_zget(string name, string[] keys)
        {
            byte[][] req = new byte[keys.Length][];
            for (int i = 0; i < keys.Length; i++)
            {
                req[i] = _bytes(keys[i]);
            }
            return this.Multi_zget(_bytes(name), req);
        }

    }

    public class Link
    {
        private TcpClient sock;
        private MemoryStream recv_buf = new MemoryStream(8 * 1024);

        public Link(string host, int port)
        {
            sock = new TcpClient(host, port);
            sock.NoDelay = true;
            sock.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
        }

        ~Link()
        {
            this.Close();
        }

        public void Close()
        {
            if (sock != null)
            {
                sock.Close();
            }
            sock = null;
        }

        public List<byte[]> Request(string cmd, params string[] args)
        {
            List<byte[]> req = new List<byte[]>(1 + args.Length);
            req.Add(Encoding.Default.GetBytes(cmd));
            foreach (string s in args)
            {
                req.Add(Encoding.Default.GetBytes(s));
            }
            return this.Request(req);
        }

        public List<byte[]> Request(string cmd, params byte[][] args)
        {
            List<byte[]> req = new List<byte[]>(1 + args.Length);
            req.Add(Encoding.Default.GetBytes(cmd));
            req.AddRange(args);
            return this.Request(req);
        }

        public List<byte[]> Request(List<byte[]> req)
        {
            MemoryStream buf = new MemoryStream();
            foreach (byte[] p in req)
            {
                byte[] len = Encoding.Default.GetBytes(p.Length.ToString());
                buf.Write(len, 0, len.Length);
                buf.WriteByte((byte)'\n');
                buf.Write(p, 0, p.Length);
                buf.WriteByte((byte)'\n');
            }
            buf.WriteByte((byte)'\n');

            byte[] bs = buf.GetBuffer();
            sock.GetStream().Write(bs, 0, (int)buf.Length);
            //Console.Write(Encoding.Default.GetString(bs, 0, (int)buf.Length));
            return Recv();
        }

        private List<byte[]> Recv()
        {
            while (true)
            {
                List<byte[]> ret = Parse();
                if (ret != null)
                {
                    return ret;
                }
                byte[] bs = new byte[8192];
                int len = sock.GetStream().Read(bs, 0, bs.Length);
                //Console.WriteLine("<< " + Encoding.Default.GetString(bs));
                recv_buf.Write(bs, 0, len);
            }
        }

        private static int Memchr(byte[] bs, byte b, int offset)
        {
            for (int i = offset; i < bs.Length; i++)
            {
                if (bs[i] == b)
                {
                    return i;
                }
            }
            return -1;
        }

        private List<byte[]> Parse()
        {
            List<byte[]> list = new List<byte[]>();
            byte[] buf = recv_buf.GetBuffer();

            int idx = 0;
            while (true)
            {
                int pos = Memchr(buf, (byte)'\n', idx);
                //System.out.println("pos: " + pos + " idx: " + idx);
                if (pos == -1)
                {
                    break;
                }
                if (pos == idx || (pos == idx + 1 && buf[idx] == '\r'))
                {
                    idx += 1; // if '\r', next time will skip '\n'
                              // ignore empty leading lines
                    if (list.Count == 0)
                    {
                        continue;
                    }
                    else
                    {
                        int left = (int)recv_buf.Length - idx;
                        recv_buf = new MemoryStream(8192);
                        if (left > 0)
                        {
                            recv_buf.Write(buf, idx, left);
                        }
                        return list;
                    }
                }
                byte[] lens = new byte[pos - idx];
                Array.Copy(buf, idx, lens, 0, lens.Length);
                int len = Int32.Parse(Encoding.Default.GetString(lens));

                idx = pos + 1;
                if (idx + len >= recv_buf.Length)
                {
                    break;
                }
                byte[] data = new byte[len];
                Array.Copy(buf, idx, data, 0, (int)data.Length);

                //Console.WriteLine("len: " + len + " data: " + Encoding.Default.GetString(data));
                idx += len + 1; // skip '\n'
                list.Add(data);
            }
            return null;
        }
    }
}
