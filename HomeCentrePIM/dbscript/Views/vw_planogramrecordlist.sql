CREATE OR REPLACE VIEW public.vw_planogramrecordlist
 AS
 SELECT DISTINCT ON (u.id) u.id,
    u.departmentid,
    u.planocode AS planoorfamilycode,
    u.status AS planoorfamilycodestatus,
    u.datecreated::date::text AS datecreated,
    to_char(u.rmsintegrationdate::date::timestamp with time zone, 'dd/MM/yyyy'::text) AS planoorfamilycodecreateddate,
    to_char(u.firstlevelapproveddate::date::timestamp with time zone, 'dd/MM/yyyy'::text) AS l1actiondate,
    u.description,
    u.firstlevelcomments,
    u.l1rejectdescription,
    COALESCE(u.firstlevelaction, 'Pending'::text) AS planoorfamilynamecreationstatusl1,
    COALESCE(u.secondlevelaction, 'Pending'::text) AS planoorfamilynamecreationstatusl2,
    to_char(u.secondlevelapproveddate::date::timestamp with time zone, 'dd/MM/yyyy'::text) AS l2actiondate,
    u.secondlevelcomments,
    u.l2rejectdescription,
    to_char(u.datecreated::date::timestamp with time zone, 'dd/MM/yyyy'::text) AS planoorfamilycreateddate,
    t.action AS test,
    t.id AS taskstepinstanceid,
    w.recordid,
    l1.name AS actionedbyl1,
    l2.name AS actionedbyl2,
    di.name AS division,
    c1.name AS planoorfamilycreatedby,
    concat(u.planogram, ' ', u.familyname) AS planoorfamilyname,
        CASE
            WHEN u.familyname = ''::text THEN 'Planogram'::text
            ELSE 'FamilyName'::text
        END AS requesttype
   FROM uidinitiation u
     JOIN workflowrun w ON u.id = w.recordid
     JOIN taskstepinstance t ON t.workflowrunid = w.id
     LEFT JOIN appuser l1 ON l1.id = u.firstlevelapprovedby
     LEFT JOIN appuser c1 ON c1.id = u.createdby
     LEFT JOIN appuser l2 ON l2.id = u.secondlevelapprovedby
     LEFT JOIN division di ON di.id = u.divisionid
  ORDER BY u.id DESC;

ALTER TABLE public.vw_planogramrecordlist
    OWNER TO lmdev_admin;
