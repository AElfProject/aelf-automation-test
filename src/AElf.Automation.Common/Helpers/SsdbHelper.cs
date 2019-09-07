using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace AElf.Automation.Common.Helpers
{
    public class SsdbHelper
    {
        public SsdbHelper(string host, int port = 8888)
        {
            Client = new SsdbClient(host, port);
        }

        public SsdbClient Client { get; set; }
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
            Close();
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
            if (_respCode != "ok") throw new Exception(_respCode);
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
            Assert_ok();

            var size = (resp.Count - 1) / 2;
            var kvs = new KeyValuePair<string, byte[]>[size];
            for (var i = 0; i < size; i += 1)
            {
                var key = _string(resp[i * 2 + 1]);
                var val = resp[i * 2 + 2];
                kvs[i] = new KeyValuePair<string, byte[]>(key, val);
            }

            return kvs;
        }

        /***** kv *****/

        public bool Exists(byte[] key)
        {
            var resp = Request("exists", key);
            _respCode = _string(resp[0]);
            if (_respCode == "not_found") return false;

            Assert_ok();
            if (resp.Count != 2) throw new Exception("Bad response!");

            return _string(resp[1]) == "1" ? true : false;
        }

        public bool Exists(string key)
        {
            return Exists(_bytes(key));
        }

        public void Set(byte[] key, byte[] val)
        {
            var resp = Request("set", key, val);
            _respCode = _string(resp[0]);
            Assert_ok();
        }

        public void Set(string key, string val)
        {
            Set(_bytes(key), _bytes(val));
        }

        /// <summary>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <returns>returns true if name.key is found, otherwise returns false.</returns>
        public bool get(byte[] key, out byte[] val)
        {
            val = null;
            var resp = Request("get", key);
            _respCode = _string(resp[0]);
            if (_respCode == "not_found") return false;

            Assert_ok();
            if (resp.Count != 2) throw new Exception("Bad response!");

            val = resp[1];
            return true;
        }

        public bool Get(string key, out byte[] val)
        {
            return get(_bytes(key), out val);
        }

        public bool Get(string key, out string val)
        {
            val = null;
            byte[] bs;
            if (!Get(key, out bs)) return false;

            val = _string(bs);
            return true;
        }

        public void del(byte[] key)
        {
            var resp = Request("del", key);
            _respCode = _string(resp[0]);
            Assert_ok();
        }

        public void del(string key)
        {
            del(_bytes(key));
        }

        public KeyValuePair<string, byte[]>[] scan(string key_start, string key_end, long limit)
        {
            var resp = Request("scan", key_start, key_end, limit.ToString());
            return Parse_scan_resp(resp);
        }

        public KeyValuePair<string, byte[]>[] rscan(string key_start, string key_end, long limit)
        {
            var resp = Request("rscan", key_start, key_end, limit.ToString());
            return Parse_scan_resp(resp);
        }

        /***** hash *****/

        public void hset(byte[] name, byte[] key, byte[] val)
        {
            var resp = Request("hset", name, key, val);
            _respCode = _string(resp[0]);
            Assert_ok();
        }

        public void hset(string name, string key, byte[] val)
        {
            hset(_bytes(name), _bytes(key), val);
        }

        public void hset(string name, string key, string val)
        {
            hset(_bytes(name), _bytes(key), _bytes(val));
        }

        /// <summary>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <returns>returns true if name.key is found, otherwise returns false.</returns>
        public bool hget(byte[] name, byte[] key, out byte[] val)
        {
            val = null;
            var resp = Request("hget", name, key);
            _respCode = _string(resp[0]);
            if (_respCode == "not_found") return false;

            Assert_ok();
            if (resp.Count != 2) throw new Exception("Bad response!");

            val = resp[1];
            return true;
        }

        public bool Hget(string name, string key, out byte[] val)
        {
            return hget(_bytes(name), _bytes(key), out val);
        }

        public bool Hget(string name, string key, out string val)
        {
            val = null;
            byte[] bs;
            if (!Hget(name, key, out bs)) return false;

            val = _string(bs);
            return true;
        }

        public void Hdel(byte[] name, byte[] key)
        {
            var resp = Request("hdel", name, key);
            _respCode = _string(resp[0]);
            Assert_ok();
        }

        public void Hdel(string name, string key)
        {
            Hdel(_bytes(name), _bytes(key));
        }

        public bool hexists(byte[] name, byte[] key)
        {
            var resp = Request("hexists", name, key);
            _respCode = _string(resp[0]);
            if (_respCode == "not_found") return false;

            Assert_ok();
            if (resp.Count != 2) throw new Exception("Bad response!");

            return _string(resp[1]) == "1" ? true : false;
        }

        public bool hexists(string name, string key)
        {
            return hexists(_bytes(name), _bytes(key));
        }

        public long hsize(byte[] name)
        {
            var resp = Request("hsize", name);
            _respCode = _string(resp[0]);
            Assert_ok();
            if (resp.Count != 2) throw new Exception("Bad response!");

            return long.Parse(_string(resp[1]));
        }

        public long hsize(string name)
        {
            return hsize(_bytes(name));
        }

        public KeyValuePair<string, byte[]>[] hscan(string name, string key_start, string key_end, long limit)
        {
            var resp = Request("hscan", name, key_start, key_end, limit.ToString());
            return Parse_scan_resp(resp);
        }

        public KeyValuePair<string, byte[]>[] hrscan(string name, string key_start, string key_end, long limit)
        {
            var resp = Request("hrscan", name, key_start, key_end, limit.ToString());
            return Parse_scan_resp(resp);
        }

        public void multi_hset(byte[] name, KeyValuePair<byte[], byte[]>[] kvs)
        {
            var req = new byte[kvs.Length * 2 + 1][];
            req[0] = name;
            for (var i = 0; i < kvs.Length; i++)
            {
                req[2 * i + 1] = kvs[i].Key;
                req[2 * i + 2] = kvs[i].Value;
            }

            var resp = Request("multi_hset", req);
            _respCode = _string(resp[0]);
            Assert_ok();
        }

        public void multi_hset(string name, KeyValuePair<string, string>[] kvs)
        {
            var req = new KeyValuePair<byte[], byte[]>[kvs.Length];
            for (var i = 0; i < kvs.Length; i++)
                req[i] = new KeyValuePair<byte[], byte[]>(_bytes(kvs[i].Key), _bytes(kvs[i].Value));

            multi_hset(_bytes(name), req);
        }

        public void multi_hdel(byte[] name, byte[][] keys)
        {
            var req = new byte[keys.Length + 1][];
            req[0] = name;
            for (var i = 0; i < keys.Length; i++) req[i + 1] = keys[i];

            var resp = Request("multi_hdel", req);
            _respCode = _string(resp[0]);
            Assert_ok();
        }

        public void multi_hdel(string name, string[] keys)
        {
            var req = new byte[keys.Length][];
            for (var i = 0; i < keys.Length; i++) req[i] = _bytes(keys[i]);

            multi_hdel(_bytes(name), req);
        }

        public KeyValuePair<string, byte[]>[] multi_hget(byte[] name, byte[][] keys)
        {
            var req = new byte[keys.Length + 1][];
            req[0] = name;
            for (var i = 0; i < keys.Length; i++) req[i + 1] = keys[i];

            var resp = Request("multi_hget", req);
            var ret = Parse_scan_resp(resp);

            return ret;
        }

        public KeyValuePair<string, byte[]>[] multi_hget(string name, string[] keys)
        {
            var req = new byte[keys.Length][];
            for (var i = 0; i < keys.Length; i++) req[i] = _bytes(keys[i]);

            return multi_hget(_bytes(name), req);
        }

        /***** zset *****/

        public void Zset(byte[] name, byte[] key, long score)
        {
            var resp = Request("zset", name, key, _bytes(score.ToString()));
            _respCode = _string(resp[0]);
            Assert_ok();
        }

        public void zset(string name, string key, long score)
        {
            Zset(_bytes(name), _bytes(key), score);
        }

        public long Zincr(byte[] name, byte[] key, long increment)
        {
            var resp = Request("zincr", name, key, _bytes(increment.ToString()));
            _respCode = _string(resp[0]);
            Assert_ok();
            if (resp.Count != 2) throw new Exception("Bad response!");

            return long.Parse(_string(resp[1]));
        }

        public long Zincr(string name, string key, long increment)
        {
            return Zincr(_bytes(name), _bytes(key), increment);
        }

        /// <summary>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="key"></param>
        /// <param name="score"></param>
        /// <returns>returns true if name.key is found, otherwise returns false.</returns>
        public bool Zget(byte[] name, byte[] key, out long score)
        {
            score = -1;
            var resp = Request("zget", name, key);
            _respCode = _string(resp[0]);
            if (_respCode == "not_found") return false;

            Assert_ok();
            if (resp.Count != 2) throw new Exception("Bad response!");

            score = long.Parse(_string(resp[1]));
            return true;
        }

        public bool Zget(string name, string key, out long score)
        {
            return Zget(_bytes(name), _bytes(key), out score);
        }

        public void Zdel(byte[] name, byte[] key)
        {
            var resp = Request("zdel", name, key);
            _respCode = _string(resp[0]);
            Assert_ok();
        }

        public void Zdel(string name, string key)
        {
            Zdel(_bytes(name), _bytes(key));
        }

        public long Zsize(byte[] name)
        {
            var resp = Request("zsize", name);
            _respCode = _string(resp[0]);
            Assert_ok();
            if (resp.Count != 2) throw new Exception("Bad response!");

            return long.Parse(_string(resp[1]));
        }

        public long Zsize(string name)
        {
            return Zsize(_bytes(name));
        }

        public bool Zexists(byte[] name, byte[] key)
        {
            var resp = Request("zexists", name, key);
            _respCode = _string(resp[0]);
            if (_respCode == "not_found") return false;

            Assert_ok();
            if (resp.Count != 2) throw new Exception("Bad response!");

            return _string(resp[1]) == "1" ? true : false;
        }

        public bool Zexists(string name, string key)
        {
            return Zexists(_bytes(name), _bytes(key));
        }

        public KeyValuePair<string, long>[] Zrange(string name, int offset, int limit)
        {
            var resp = Request("zrange", name, offset.ToString(), limit.ToString());
            var kvs = Parse_scan_resp(resp);
            var ret = new KeyValuePair<string, long>[kvs.Length];
            for (var i = 0; i < kvs.Length; i++)
            {
                var key = kvs[i].Key;
                var score = long.Parse(_string(kvs[i].Value));
                ret[i] = new KeyValuePair<string, long>(key, score);
            }

            return ret;
        }

        public KeyValuePair<string, long>[] Zrrange(string name, int offset, int limit)
        {
            var resp = Request("zrrange", name, offset.ToString(), limit.ToString());
            var kvs = Parse_scan_resp(resp);
            var ret = new KeyValuePair<string, long>[kvs.Length];
            for (var i = 0; i < kvs.Length; i++)
            {
                var key = kvs[i].Key;
                var score = long.Parse(_string(kvs[i].Value));
                ret[i] = new KeyValuePair<string, long>(key, score);
            }

            return ret;
        }

        public KeyValuePair<string, long>[] Zscan(string name, string key_start, long score_start, long score_end,
            long limit)
        {
            var score_s = "";
            var score_e = "";
            if (score_start != long.MinValue) score_s = score_start.ToString();

            if (score_end != long.MaxValue) score_e = score_end.ToString();

            var resp = Request("zscan", name, key_start, score_s, score_e, limit.ToString());
            var kvs = Parse_scan_resp(resp);
            var ret = new KeyValuePair<string, long>[kvs.Length];
            for (var i = 0; i < kvs.Length; i++)
            {
                var key = kvs[i].Key;
                var score = long.Parse(_string(kvs[i].Value));
                ret[i] = new KeyValuePair<string, long>(key, score);
            }

            return ret;
        }

        public KeyValuePair<string, long>[] Zrscan(string name, string key_start, long score_start, long score_end,
            long limit)
        {
            var score_s = "";
            var score_e = "";
            if (score_start != long.MaxValue) score_s = score_start.ToString();

            if (score_end != long.MinValue) score_e = score_end.ToString();

            var resp = Request("zrscan", name, key_start, score_s, score_e, limit.ToString());
            var kvs = Parse_scan_resp(resp);
            var ret = new KeyValuePair<string, long>[kvs.Length];
            for (var i = 0; i < kvs.Length; i++)
            {
                var key = kvs[i].Key;
                var score = long.Parse(_string(kvs[i].Value));
                ret[i] = new KeyValuePair<string, long>(key, score);
            }

            return ret;
        }

        public void Multi_zset(byte[] name, KeyValuePair<byte[], long>[] kvs)
        {
            var req = new byte[kvs.Length * 2 + 1][];
            req[0] = name;
            for (var i = 0; i < kvs.Length; i++)
            {
                req[2 * i + 1] = kvs[i].Key;
                req[2 * i + 2] = _bytes(kvs[i].Value.ToString());
            }

            var resp = Request("multi_zset", req);
            _respCode = _string(resp[0]);
            Assert_ok();
        }

        public void Multi_zset(string name, KeyValuePair<string, long>[] kvs)
        {
            var req = new KeyValuePair<byte[], long>[kvs.Length];
            for (var i = 0; i < kvs.Length; i++)
                req[i] = new KeyValuePair<byte[], long>(_bytes(kvs[i].Key), kvs[i].Value);

            Multi_zset(_bytes(name), req);
        }

        public void Multi_zdel(byte[] name, byte[][] keys)
        {
            var req = new byte[keys.Length + 1][];
            req[0] = name;
            for (var i = 0; i < keys.Length; i++) req[i + 1] = keys[i];

            var resp = Request("multi_zdel", req);
            _respCode = _string(resp[0]);
            Assert_ok();
        }

        public void Multi_zdel(string name, string[] keys)
        {
            var req = new byte[keys.Length][];
            for (var i = 0; i < keys.Length; i++) req[i] = _bytes(keys[i]);

            Multi_zdel(_bytes(name), req);
        }

        public KeyValuePair<string, long>[] Multi_zget(byte[] name, byte[][] keys)
        {
            var req = new byte[keys.Length + 1][];
            req[0] = name;
            for (var i = 0; i < keys.Length; i++) req[i + 1] = keys[i];

            var resp = Request("multi_zget", req);
            var kvs = Parse_scan_resp(resp);
            var ret = new KeyValuePair<string, long>[kvs.Length];
            for (var i = 0; i < kvs.Length; i++)
            {
                var key = kvs[i].Key;
                var score = long.Parse(_string(kvs[i].Value));
                ret[i] = new KeyValuePair<string, long>(key, score);
            }

            return ret;
        }

        public KeyValuePair<string, long>[] Multi_zget(string name, string[] keys)
        {
            var req = new byte[keys.Length][];
            for (var i = 0; i < keys.Length; i++) req[i] = _bytes(keys[i]);

            return Multi_zget(_bytes(name), req);
        }
    }

    public class Link
    {
        private MemoryStream recv_buf = new MemoryStream(8 * 1024);
        private TcpClient sock;

        public Link(string host, int port)
        {
            sock = new TcpClient(host, port);
            sock.NoDelay = true;
            sock.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
        }

        ~Link()
        {
            Close();
        }

        public void Close()
        {
            if (sock != null) sock.Close();

            sock = null;
        }

        public List<byte[]> Request(string cmd, params string[] args)
        {
            var req = new List<byte[]>(1 + args.Length);
            req.Add(Encoding.Default.GetBytes(cmd));
            foreach (var s in args) req.Add(Encoding.Default.GetBytes(s));

            return Request(req);
        }

        public List<byte[]> Request(string cmd, params byte[][] args)
        {
            var req = new List<byte[]>(1 + args.Length);
            req.Add(Encoding.Default.GetBytes(cmd));
            req.AddRange(args);
            return Request(req);
        }

        public List<byte[]> Request(List<byte[]> req)
        {
            var buf = new MemoryStream();
            foreach (var p in req)
            {
                var len = Encoding.Default.GetBytes(p.Length.ToString());
                buf.Write(len, 0, len.Length);
                buf.WriteByte((byte) '\n');
                buf.Write(p, 0, p.Length);
                buf.WriteByte((byte) '\n');
            }

            buf.WriteByte((byte) '\n');

            var bs = buf.GetBuffer();
            sock.GetStream().Write(bs, 0, (int) buf.Length);
            //Console.Write(Encoding.Default.GetString(bs, 0, (int)buf.Length));
            return Recv();
        }

        private List<byte[]> Recv()
        {
            while (true)
            {
                var ret = Parse();
                if (ret != null) return ret;

                var bs = new byte[8192];
                var len = sock.GetStream().Read(bs, 0, bs.Length);
                //Console.WriteLine("<< " + Encoding.Default.GetString(bs));
                recv_buf.Write(bs, 0, len);
            }
        }

        private static int Memchr(byte[] bs, byte b, int offset)
        {
            for (var i = offset; i < bs.Length; i++)
                if (bs[i] == b)
                    return i;

            return -1;
        }

        private List<byte[]> Parse()
        {
            var list = new List<byte[]>();
            var buf = recv_buf.GetBuffer();

            var idx = 0;
            while (true)
            {
                var pos = Memchr(buf, (byte) '\n', idx);
                //System.out.println("pos: " + pos + " idx: " + idx);
                if (pos == -1) break;

                if (pos == idx || pos == idx + 1 && buf[idx] == '\r')
                {
                    idx += 1; // if '\r', next time will skip '\n'
                    // ignore empty leading lines
                    if (list.Count == 0)
                    {
                        continue;
                    }

                    var left = (int) recv_buf.Length - idx;
                    recv_buf = new MemoryStream(8192);
                    if (left > 0) recv_buf.Write(buf, idx, left);

                    return list;
                }

                var lens = new byte[pos - idx];
                Array.Copy(buf, idx, lens, 0, lens.Length);
                var len = int.Parse(Encoding.Default.GetString(lens));

                idx = pos + 1;
                if (idx + len >= recv_buf.Length) break;

                var data = new byte[len];
                Array.Copy(buf, idx, data, 0, data.Length);

                //Console.WriteLine("len: " + len + " data: " + Encoding.Default.GetString(data));
                idx += len + 1; // skip '\n'
                list.Add(data);
            }

            return null;
        }
    }
}