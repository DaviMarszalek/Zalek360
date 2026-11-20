BEGIN READ ONLY;
SELECT jsonb_build_object(
'tables', (SELECT jsonb_agg(jsonb_build_object('table',t.table_name,'type',t.table_type) ORDER BY t.table_name)
 FROM information_schema.tables t WHERE t.table_schema='public' AND t.table_name <> '__EFMigrationsHistory'),
'columns', (SELECT jsonb_agg(to_jsonb(x) ORDER BY x."table",x.ordinal_position) FROM (
 SELECT c.table_name AS "table",c.column_name AS name,c.ordinal_position,c.data_type,c.udt_schema,c.udt_name,
 c.character_maximum_length,c.numeric_precision,c.numeric_precision_radix,c.numeric_scale,c.datetime_precision,c.interval_type,c.interval_precision,
 c.is_nullable,c.column_default,c.collation_schema,c.collation_name,c.domain_schema,c.domain_name,
 c.is_identity,c.identity_generation,c.identity_start,c.identity_increment,c.identity_minimum,c.identity_maximum,c.identity_cycle,c.is_generated,c.generation_expression
 FROM information_schema.columns c WHERE c.table_schema='public' AND c.table_name <> '__EFMigrationsHistory') x),
'constraints', (SELECT jsonb_agg(to_jsonb(x) ORDER BY x."table",x.kind,x.name) FROM (
 SELECT t.relname AS "table",c.conname AS name,c.contype::text AS kind,pg_get_constraintdef(c.oid,true) AS definition,
 ARRAY(SELECT a.attname FROM unnest(c.conkey) WITH ORDINALITY k(num,ord) JOIN pg_attribute a ON a.attrelid=c.conrelid AND a.attnum=k.num ORDER BY k.ord) AS columns,
 rt.relname AS ref_table,rn.nspname AS ref_schema,
 ARRAY(SELECT a.attname FROM unnest(c.confkey) WITH ORDINALITY k(num,ord) JOIN pg_attribute a ON a.attrelid=c.confrelid AND a.attnum=k.num ORDER BY k.ord) AS ref_columns,
 c.confupdtype::text AS on_update,c.confdeltype::text AS on_delete,c.confmatchtype::text AS match,
 c.condeferrable AS deferrable,c.condeferred AS initially_deferred,c.convalidated AS validated,c.connoinherit AS no_inherit,c.conislocal AS is_local
 FROM pg_constraint c JOIN pg_class t ON t.oid=c.conrelid JOIN pg_namespace n ON n.oid=t.relnamespace
 LEFT JOIN pg_class rt ON rt.oid=c.confrelid LEFT JOIN pg_namespace rn ON rn.oid=rt.relnamespace
 WHERE n.nspname='public' AND t.relname <> '__EFMigrationsHistory') x),
'indexes', (SELECT jsonb_agg(to_jsonb(x) ORDER BY x."table",x.name) FROM (
 SELECT t.relname AS "table",ic.relname AS name,am.amname AS method,i.indisunique AS "unique",i.indisprimary AS "primary",
 i.indisexclusion AS exclusion,i.indimmediate AS immediate,i.indisclustered AS clustered,i.indisvalid AS valid,
 i.indisready AS ready,i.indislive AS live,i.indisreplident AS replica_identity,i.indnullsnotdistinct AS nulls_not_distinct,
 i.indnkeyatts AS key_attributes,
 ARRAY(SELECT pg_get_indexdef(i.indexrelid,k,true) FROM generate_series(1,i.indnatts) k) AS columns,
 i.indoption::text AS options,pg_get_expr(i.indpred,i.indrelid,true) AS filter,ic.reloptions AS reloptions,
 ts.spcname AS tablespace,pg_get_indexdef(i.indexrelid) AS definition
 FROM pg_index i JOIN pg_class t ON t.oid=i.indrelid JOIN pg_namespace n ON n.oid=t.relnamespace
 JOIN pg_class ic ON ic.oid=i.indexrelid JOIN pg_am am ON am.oid=ic.relam LEFT JOIN pg_tablespace ts ON ts.oid=ic.reltablespace
 WHERE n.nspname='public' AND t.relname <> '__EFMigrationsHistory') x),
'sequences', (SELECT coalesce(jsonb_agg(to_jsonb(x) ORDER BY x.name),'[]'::jsonb) FROM (
 SELECT sequencename AS name,sequenceowner AS owner,data_type::text,start_value,min_value,max_value,increment_by,cycle,cache_size
 FROM pg_sequences WHERE schemaname='public') x)
);
COMMIT;

