-- FUNCTION: public.update_product_columndata_batch(jsonb[])

-- DROP FUNCTION IF EXISTS public.update_product_columndata_batch(jsonb[]);

CREATE OR REPLACE FUNCTION public.update_product_columndata_batch(
	p_columndata_array jsonb[])
    RETURNS TABLE(updated_records jsonb) 
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
    ROWS 2000

AS $BODY$
DECLARE
    updated_record JSONB;
BEGIN
    FOR i IN 1..array_length(p_columndata_array, 1) LOOP
        UPDATE product
        SET
            columndata = jsonb_set(
                            jsonb_set(
                                jsonb_set(
                                    jsonb_set(
                                        jsonb_set(
                                            jsonb_set(
                                                columndata,
                                                '{Division Name}',
                                                (
                                                    SELECT TO_JSONB(REPLACE(div_name, '"', ''))::JSONB
                                                    FROM merchandise_hierarchy
                                                    WHERE division = (p_columndata_array[i]->>'Division Code')::BIGINT
                                                    LIMIT 1
                                                )
                                            ),
                                            '{Dept Name}',
                                            (
                                                SELECT TO_JSONB(REPLACE(dept_name, '"', ''))::JSONB
                                                FROM merchandise_hierarchy
                                                WHERE dept = (p_columndata_array[i]->>'Dept Code')::BIGINT
                                                    AND division = (p_columndata_array[i]->>'Division Code')::BIGINT
                                                LIMIT 1
                                            )
                                        ),
                                        '{Group Name}',
                                        (
                                            SELECT TO_JSONB(REPLACE(group_name, '"', ''))::JSONB
                                            FROM merchandise_hierarchy
                                            WHERE group_no = (p_columndata_array[i]->>'Group Code')::BIGINT
                                            LIMIT 1
                                        )
                                    ),
                                    '{Subclass Name}',
                                    (
                                        SELECT TO_JSONB(REPLACE(sub_name, '"', ''))::JSONB
                                        FROM merchandise_hierarchy
                                        WHERE subclass = (p_columndata_array[i]->>'Subclass Code')::BIGINT
                                            AND dept = (p_columndata_array[i]->>'Dept Code')::BIGINT
                                            AND class = (p_columndata_array[i]->>'Class Code')::BIGINT
                                            AND division = (p_columndata_array[i]->>'Division Code')::BIGINT
                                        LIMIT 1
                                    )
                                ),
                                '{Class Name}',
                                (
                                    SELECT TO_JSONB(REPLACE(class_name, '"', ''))::JSONB
                                    FROM merchandise_hierarchy
                                    WHERE class = (p_columndata_array[i]->>'Class Code')::BIGINT
                                        AND dept = (p_columndata_array[i]->>'Dept Code')::BIGINT
                                        AND division = (p_columndata_array[i]->>'Division Code')::BIGINT
                                    LIMIT 1
                                )
                            ),
                            '{UID Status}',
                            '"Active"'
                        ),
            status = '0'
        WHERE
            columndata @> p_columndata_array[i]
            AND columndata->>'Uid' = p_columndata_array[i]->>'Uid'
        RETURNING columndata INTO updated_record;

        RETURN QUERY SELECT updated_record;
    END LOOP;
END;
$BODY$;

ALTER FUNCTION public.update_product_columndata_batch(jsonb[])
    OWNER TO pim_prod_admin;
