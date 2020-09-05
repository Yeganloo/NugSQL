-- :name create_tbl_user :affected

-- Table: test.user

DROP TABLE IF EXISTS test."user";

CREATE TABLE test."user"
(
    id serial NOT NULL,
    user_name character varying(150) NOT NULL,
    salt bytea NOT NULL,
    password bytea NOT NULL,
    profile jsonb NOT NULL,
    status smallint NOT NULL,
    CONSTRAINT user_pkey PRIMARY KEY (id),
    CONSTRAINT user_user_name_key UNIQUE (user_name)
);

ALTER TABLE test."user"
    OWNER to postgres;