-- :name create_schema_test :affected
--DROP SCHEMA IF EXISTS test;

IF NOT EXISTS (SELECT name FROM sys.schemas WHERE name = N'test')
BEGIN

EXEC('CREATE SCHEMA test
    AUTHORIZATION DNNPro_ir_hydb2016');

END
    