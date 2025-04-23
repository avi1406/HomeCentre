CREATE OR REPLACE VIEW public.vw_nameidcollection
 AS
 SELECT productdepartment.name,
    productdepartment.departmentcode AS id,
    'departmentcode'::text AS attribute
   FROM productdepartment
UNION
 SELECT division.name,
    division.code::bigint AS id,
    'code'::text AS attribute
   FROM division
UNION
 SELECT brand.name,
    brand.brandcode AS id,
    'brandcode'::text AS attribute
   FROM brand
UNION
 SELECT productsubclass.name,
    productsubclass.code::bigint AS id,
    'subclasscode'::text AS attribute
   FROM productsubclass
UNION
 SELECT productclass.name,
    productclass.code::bigint AS id,
    'classcode'::text AS attribute
   FROM productclass
UNION
 SELECT productgroup.name,
    productgroup.groupcode AS id,
    'groupcode'::text AS attribute
   FROM productgroup
UNION
 SELECT business.name,
    business.businesscode AS id,
    'Businesscode'::text AS attribute
   FROM business;

ALTER TABLE public.vw_nameidcollection
    OWNER TO lmdev_admin;