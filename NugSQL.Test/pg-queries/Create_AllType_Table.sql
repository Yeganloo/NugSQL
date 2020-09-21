-- :name create_tbl_allTypes :affected

-- Table: test.user

DROP TABLE IF EXISTS test."all_types";

CREATE TABLE test."all_types"
(
    "my_int64" bigint NOT NULL,
    "my_int32" int NOT NULL,
    "my_int16" smallint NOT NULL,
    "my_string" character varying(150) NOT NULL,
    "my_bytes" bytea NOT NULL,
    "my_bool" boolean NOT NULL,
    "my_json" jsonb NOT NULL,
    "my_single" real NOT NULL,
    "my_double" double precision NOT NULL,
    "my_bits" bit varying(10) NOT NULL,
    "my_guid" uuid NOT NULL,
    "my_datetime" timestamp NOT NULL
);

ALTER TABLE test."all_types"
    OWNER to postgres;