﻿//------------------------------------------------------------------------------
// <auto-generated>
//     このコードはツールによって生成されました。
//     ランタイム バージョン:4.0.30319.42000
//
//     このファイルへの変更は、以下の状況下で不正な動作の原因になったり、
//     コードが再生成されるときに損失したりします。
// </auto-generated>
//------------------------------------------------------------------------------

namespace Db2Source.DataSet.Properties {
    using System;
    
    
    /// <summary>
    ///   ローカライズされた文字列などを検索するための、厳密に型指定されたリソース クラスです。
    /// </summary>
    // このクラスは StronglyTypedResourceBuilder クラスが ResGen
    // または Visual Studio のようなツールを使用して自動生成されました。
    // メンバーを追加または削除するには、.ResX ファイルを編集して、/str オプションと共に
    // ResGen を実行し直すか、または VS プロジェクトをビルドし直します。
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   このクラスで使用されているキャッシュされた ResourceManager インスタンスを返します。
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Db2Source.DataSet.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   厳密に型指定されたこのリソース クラスを使用して、すべての検索リソースに対し、
        ///   現在のスレッドの CurrentUICulture プロパティをオーバーライドします。
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   select pg_client_encoding() に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string ClientEncoding_SQL {
            get {
                return ResourceManager.GetString("ClientEncoding_SQL", resourceCulture);
            }
        }
        
        /// <summary>
        ///   select pg_encoding_to_char(c.contoencoding) as encoding
        ///from pg_database db
        ///  join pg_conversion c on (db.encoding = c.conforencoding)
        ///where db.datname = current_database()
        ///order by pg_encoding_to_char(c.contoencoding) に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string GetEncodings_SQL {
            get {
                return ResourceManager.GetString("GetEncodings_SQL", resourceCulture);
            }
        }
        
        /// <summary>
        ///   select current_schema() に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string NpgsqlDataSet_USERINFOSQL {
            get {
                return ResourceManager.GetString("NpgsqlDataSet_USERINFOSQL", resourceCulture);
            }
        }
        
        /// <summary>
        ///   select
        ///  a.attrelid, a.attname, a.atttypid, a.attstattarget, a.attlen, a.attnum, a.attndims, a.attcacheoff, 
        ///  a.atttypmod, a.attbyval, a.attstorage, a.attalign, a.attnotnull, a.atthasdef, a.attisdropped, 
        ///  a.attislocal, a.attinhcount, a.attcollation,
        ///  format_type(a.atttypid, a.atttypmod) as formattype,
        ///  pg_get_expr(ad.adbin, ad.adrelid) as defaultexpr
        ///from pg_catalog.pg_attribute a
        ///  left outer join pg_catalog.pg_attrdef ad on ((a.attrelid = ad.adrelid) AND (a.attnum = ad.adnum))
        ///where a.atttypi [残りの文字列は切り詰められました]&quot;; に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string PgAttribute_SQL {
            get {
                return ResourceManager.GetString("PgAttribute_SQL", resourceCulture);
            }
        }
        
        /// <summary>
        ///   select i.indexrelid as oid, i.indrelid, i.indnatts,
        ///  i.indisunique, i.indisprimary, i.indisexclusion, i.indimmediate, i.indisclustered,
        ///  i.indisvalid, i.indcheckxmin, i.indisready, i.indislive, i.indisreplident,
        ///  i.indkey, i.indcollation, i.indclass, i.indoption,
        ///  pg_get_indexdef(i.indexrelid) AS indexdef,
        ///  am.amname AS indextype
        ///from pg_index i
        ///  left outer join pg_opclass op0 on (i.indclass[0] = op0.oid)
        ///  left outer join pg_am am on (op0.opcmethod = am.oid) に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string PgClass_INDEXSQL {
            get {
                return ResourceManager.GetString("PgClass_INDEXSQL", resourceCulture);
            }
        }
        
        /// <summary>
        ///   select c.oid,
        ///  p.start_value, p.minimum_value, p.maximum_value, p.increment, p.cycle_option,
        ///  rn.nspname as owned_schema, rc.relname as owned_table, ra.attname as owned_field
        ///from pg_class c
        ///  left outer join pg_depend d on (c.oid = d.objid and d.deptype = &apos;a&apos;)
        ///  left outer join pg_class rc on (d.refobjid = rc.oid)
        ///  left outer join pg_attribute ra on (d.refobjid = ra.attrelid and d.refobjsubid = ra.attnum)
        ///  left outer join pg_namespace rn on (rc.relnamespace = rn.oid),
        ///  lateral pg_sequence_para [残りの文字列は切り詰められました]&quot;; に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string PgClass_SEQUENCESQL {
            get {
                return ResourceManager.GetString("PgClass_SEQUENCESQL", resourceCulture);
            }
        }
        
        /// <summary>
        ///   select oid,
        ///  relname, relnamespace, reltablespace, relkind,
        ///  pg_get_userbyid(relowner) as ownername
        ///from pg_catalog.pg_class
        ///where not pg_is_other_temp_schema(relnamespace)
        ///  and (not relkind in (&apos;c&apos;, &apos;r&apos;, &apos;v&apos;, &apos;f&apos;)
        ///    or pg_has_role(relowner, &apos;USAGE&apos;::text)
        ///    or has_table_privilege(oid, &apos;SELECT, INSERT, UPDATE, DELETE, TRUNCATE, REFERENCES, TRIGGER&apos;)
        ///    or has_any_column_privilege(oid, &apos;SELECT, INSERT, UPDATE, REFERENCES&apos;)) に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string PgClass_SQL {
            get {
                return ResourceManager.GetString("PgClass_SQL", resourceCulture);
            }
        }
        
        /// <summary>
        ///   select oid, pg_get_viewdef(oid) viewdef from pg_class where relkind = &apos;v&apos;::&quot;char&quot; に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string PgClass_VIEWDEFSQL {
            get {
                return ResourceManager.GetString("PgClass_VIEWDEFSQL", resourceCulture);
            }
        }
        
        /// <summary>
        ///   select r.oid
        ///from pg_catalog.pg_class r
        ///  inner join pg_catalog.pg_namespace ns
        ///    on (r.relnamespace = ns.oid and not pg_is_other_temp_schema(r.relnamespace) and ns.nspname = :schema)
        ///where r.relname = :name and r.relkind = :kind
        ///  and (not r.relkind in (&apos;c&apos;, &apos;r&apos;, &apos;v&apos;, &apos;f&apos;)
        ///    or pg_has_role(r.relowner, &apos;USAGE&apos;::text)
        ///    or has_table_privilege(r.oid, &apos;SELECT, INSERT, UPDATE, DELETE, TRUNCATE, REFERENCES, TRIGGER&apos;)
        ///    or has_any_column_privilege(r.oid, &apos;SELECT, INSERT, UPDATE, REFERENCES&apos;))
        /// に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string PgClassOid_SQL {
            get {
                return ResourceManager.GetString("PgClassOid_SQL", resourceCulture);
            }
        }
        
        /// <summary>
        ///   select oid, substring(pg_get_constraintdef(oid), 7) as checkdef
        ///from pg_catalog.pg_constraint where contype = &apos;c&apos; に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string PgConstraint_CHECKSQL {
            get {
                return ResourceManager.GetString("PgConstraint_CHECKSQL", resourceCulture);
            }
        }
        
        /// <summary>
        ///   select oid,
        ///  conname, connamespace, contype, condeferrable, condeferred, conrelid, conindid, confrelid, confupdtype, confdeltype, conkey, confkey
        ///from pg_catalog.pg_constraint に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string PgConstraint_SQL {
            get {
                return ResourceManager.GetString("PgConstraint_SQL", resourceCulture);
            }
        }
        
        /// <summary>
        ///   select
        ///  d.oid, d.datname,
        ///  d.datdba, u.usename as dbaname,
        ///  d.&quot;encoding&quot;, pg_encoding_to_char(d.&quot;encoding&quot;) as encoding_char,
        ///  d.datcollate, d.datctype, d.datistemplate, d.datallowconn, 
        ///  d.datconnlimit, d.datlastsysoid, d.datfrozenxid, d.datminmxid,
        ///  d.dattablespace, ts.spcname as dattablespacename,
        ///  version() as version
        ///from pg_database as d
        ///  left outer join pg_user u on (d.datdba = u.usesysid)
        ///  left outer join pg_tablespace ts on (d.dattablespace = ts.oid)
        ///where not d.datistemplate に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string PgDatabase_SQL {
            get {
                return ResourceManager.GetString("PgDatabase_SQL", resourceCulture);
            }
        }
        
        /// <summary>
        ///   select *
        ///from pg_catalog.pg_description に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string PgDescription_SQL {
            get {
                return ResourceManager.GetString("PgDescription_SQL", resourceCulture);
            }
        }
        
        /// <summary>
        ///   select ns.oid, ns.nspname, ns.nspowner from pg_namespace ns order by ns.nspname に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string PgNamespace_SQL {
            get {
                return ResourceManager.GetString("PgNamespace_SQL", resourceCulture);
            }
        }
        
        /// <summary>
        ///   select ss.p_oid as oid, (ss.x).n,
        ///  pg_get_function_arg_default(ss.p_oid, (ss.x).n) as parameter_default
        ///from (select p.oid as p_oid, information_schema._pg_expandarray(coalesce(p.proallargtypes, (p.proargtypes)::oid[])) as x from pg_proc p) ss
        ///where pg_get_function_arg_default(ss.p_oid, (ss.x).n) is not null に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string PgProc_ARGDEFAULTSQL {
            get {
                return ResourceManager.GetString("PgProc_ARGDEFAULTSQL", resourceCulture);
            }
        }
        
        /// <summary>
        ///   select p.oid,
        ///  p.proname, p.pronamespace, p.proretset, p.prorettype, p.proargtypes, p.proallargtypes, p.proargmodes, 
        ///  p.proargnames, p.prosrc,
        ///  l.lanname, pg_get_userbyid(p.proowner) as ownername
        ///from pg_catalog.pg_proc p
        ///  left outer join pg_catalog.pg_language l on (p.prolang = l.oid)
        ///where (pg_has_role(p.proowner, &apos;USAGE&apos;) or has_function_privilege(p.oid, &apos;EXECUTE&apos;))
        ///order by p.proname, p.pronargs, p.oid に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string PgProc_SQL {
            get {
                return ResourceManager.GetString("PgProc_SQL", resourceCulture);
            }
        }
        
        /// <summary>
        ///   select p.oid, p.proname,
        ///  p.pronargs, p.prorettype, p.proargtypes, p.proallargtypes, p.proargmodes, 
        ///  p.proargnames, p.protrftypes, prosrc, probin, proconfig
        ///from pg_catalog.pg_proc p
        ///  join pg_catalog.pg_namespace ns on (p.pronamespace = ns.oid and ns.nspname = :schema)
        ///where p.proname = :name に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string PgProcOid_SQL {
            get {
                return ResourceManager.GetString("PgProcOid_SQL", resourceCulture);
            }
        }
        
        /// <summary>
        ///   select * from pg_roles order by rolname に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string PgRoles_SQL {
            get {
                return ResourceManager.GetString("PgRoles_SQL", resourceCulture);
            }
        }
        
        /// <summary>
        ///   select
        ///  &quot;name&quot;, setting, unit, category, short_desc, extra_desc, context, vartype, &quot;source&quot;, 
        ///  min_val, max_val, enumvals, boot_val, reset_val, sourcefile, sourceline, pending_restart
        ///from pg_settings
        ///order by &quot;name&quot;
        /// に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string PgSettings_SQL {
            get {
                return ResourceManager.GetString("PgSettings_SQL", resourceCulture);
            }
        }
        
        /// <summary>
        ///   select ts.oid, ts.spcname, ts.spcowner, pg_tablespace_location(ts.oid) as location
        ///from pg_catalog.pg_tablespace ts
        ///order by spcname に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string PgTablespace_SQL {
            get {
                return ResourceManager.GetString("PgTablespace_SQL", resourceCulture);
            }
        }
        
        /// <summary>
        ///   select oid,
        ///  tgrelid, tgname, tgfoid, tgtype, tgisinternal, tgattr,
        ///  pg_catalog.pg_get_triggerdef(oid) as triggerdef
        ///from pg_catalog.pg_trigger に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string PgTrigger_SQL {
            get {
                return ResourceManager.GetString("PgTrigger_SQL", resourceCulture);
            }
        }
        
        /// <summary>
        ///   select t.oid,
        ///  t.typname, t.typnamespace, t.typowner, t.typlen, t.typbyval, t.typtype, t.typcategory, t.typispreferred,
        ///  t.typisdefined, t.typdelim, t.typrelid, t.typelem, t.typarray,
        ///  t.typinput::text as typinput, t.typoutput::text as typoutput,
        ///  t.typreceive::text as typreceive, t.typsend::text as typsend,
        ///  t.typmodin::text as typmodin, t.typmodout::text as typmodout,
        ///  t.typanalyze::text as typanalyze, t.typalign, t.typstorage, t.typnotnull,
        ///  t.typbasetype, t.typtypmod, t.typndims, t.typcoll [残りの文字列は切り詰められました]&quot;; に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string PgType_SQL {
            get {
                return ResourceManager.GetString("PgType_SQL", resourceCulture);
            }
        }
        
        /// <summary>
        ///   A
        ///ABORT
        ///ABS
        ///ABSOLUTE
        ///ACCESS
        ///ACTION
        ///ADA
        ///ADD
        ///ADMIN
        ///AFTER
        ///AGGREGATE
        ///ALIAS
        ///ALL
        ///ALLOCATE
        ///ALSO
        ///ALTER
        ///ALWAYS
        ///ANALYSE
        ///ANALYZE
        ///AND
        ///ANY
        ///ARE
        ///ARRAY
        ///AS
        ///ASC
        ///ASENSITIVE
        ///ASSERTION
        ///ASSIGNMENT
        ///ASYMMETRIC
        ///AT
        ///ATOMIC
        ///ATTRIBUTE
        ///ATTRIBUTES
        ///AUTHORIZATION
        ///AVG
        ///BACKWARD
        ///BASE64
        ///BEFORE
        ///BEGIN
        ///BERNOULLI
        ///BETWEEN
        ///BIGINT
        ///BINARY
        ///BIT
        ///BITVAR
        ///BIT_LENGTH
        ///BLOB
        ///BOOLEAN
        ///BOTH
        ///BREADTH
        ///BY
        ///C
        ///CACHE
        ///CALL
        ///CALLED
        ///CARDINALITY
        ///CASCADE
        ///CASCADED
        ///CASE
        ///CAST
        ///CATALOG
        ///CATALOG_NAME
        ///CEIL
        ///CEILING
        ///CHAIN
        ///C [残りの文字列は切り詰められました]&quot;; に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string PostgresReservedWords {
            get {
                return ResourceManager.GetString("PostgresReservedWords", resourceCulture);
            }
        }
        
        /// <summary>
        ///   select pg_encoding_to_char(db.encoding) from pg_database db
        ///where db.datname = current_database() に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string ServerEncoding_SQL {
            get {
                return ResourceManager.GetString("ServerEncoding_SQL", resourceCulture);
            }
        }
    }
}
