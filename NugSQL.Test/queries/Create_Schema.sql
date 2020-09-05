-- :name create_schema_test :affected
DROP SCHEMA IF EXISTS test ;

CREATE SCHEMA test
    AUTHORIZATION postgres;

GRANT ALL ON SCHEMA test TO postgres;