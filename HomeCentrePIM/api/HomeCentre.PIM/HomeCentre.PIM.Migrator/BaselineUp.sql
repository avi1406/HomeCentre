


--
-- Name: postgis; Type: EXTENSION; Schema: -; Owner: 
--

-- CREATE EXTENSION IF NOT EXISTS postgis WITH SCHEMA public;


----
---- Name: EXTENSION postgis; Type: COMMENT; Schema: -; Owner: 
----

--COMMENT ON EXTENSION postgis IS 'PostGIS geometry, geography, and raster spatial types and functions';


--
-- Name: update_modified_column(); Type: FUNCTION; Schema: public; Owner: scriptuser
--

CREATE OR REPLACE FUNCTION public.update_modified_column() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    NEW.DateModified = now();
    RETURN NEW;
END;
$$;


ALTER FUNCTION public.update_modified_column() OWNER TO current_user;

