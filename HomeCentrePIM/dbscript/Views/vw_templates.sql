CREATE OR REPLACE VIEW public.vw_templates
 AS
 SELECT t.id,
    t.createdby,
    t.updatedby,
    t.datecreated,
    t.datemodified,
    t.isactive,
    t.name,
    t.departmentid,
    t.divisionid,
    t.templatestagetypeid,
    t."order",
    t.issequential,
    t.isuidtemplate,
    t.templatestatusid,
    ar.appuserid
   FROM template t
     JOIN templaterole tr ON tr.templateid = t.id
     JOIN appuserrole ar ON ar.roleid = tr.roleid;

ALTER TABLE public.vw_templates
    OWNER TO lmdev_admin;
