-- =========================================================================
-- Use Case: S001 Database Sanity & Verification Checks
-- Author: 钮培源 
-- Date: 2026-07-22 (revised)


-- WHENEVER SQLERROR EXIT FAILURE + RAISE_APPLICATION_ERROR make every check fail-fast; 
-- The script exits with a non-zero status on the first violation, so it can gate CI / a pre-demo freeze.
-- The SELECTs print the evidence for a human reading the log.
-- ==========================================

WHENEVER SQLERROR EXIT FAILURE
ALTER SESSION SET NLS_LENGTH_SEMANTICS = CHAR;

PROMPT ==========================================
PROMPT 1. DEMO FLOW TABLE COUNTS (EXPECTED: 1, 1, 1, 1, 2, 1)
PROMPT ==========================================
SELECT 
    (SELECT COUNT(*) FROM TRAINING_REQUESTS) AS REQ_COUNT,
    (SELECT COUNT(*) FROM TRAINING_REGISTRATIONS) AS REG_COUNT,
    (SELECT COUNT(*) FROM TRAINING_ATTENDANCE) AS ATTEND_COUNT,
    (SELECT COUNT(*) FROM TRAINER_RATINGS) AS RATING_COUNT,
    (SELECT COUNT(*) FROM TRAINING_TESTS) AS TEST_COUNT,
    (SELECT COUNT(*) FROM TRAINING_CERTIFICATES) AS CERT_COUNT
FROM DUAL;

DECLARE
    v_bad NUMBER;
BEGIN
    SELECT CASE WHEN
        (SELECT COUNT(*) FROM TRAINING_REQUESTS)      = 1 AND
        (SELECT COUNT(*) FROM TRAINING_REGISTRATIONS) = 1 AND
        (SELECT COUNT(*) FROM TRAINING_ATTENDANCE)    = 1 AND
        (SELECT COUNT(*) FROM TRAINER_RATINGS)        = 1 AND
        (SELECT COUNT(*) FROM TRAINING_TESTS)         = 2 AND
        (SELECT COUNT(*) FROM TRAINING_CERTIFICATES)  = 1
    THEN 0 ELSE 1 END
    INTO v_bad FROM DUAL;
    IF v_bad > 0 THEN
        RAISE_APPLICATION_ERROR(-20001, 'Demo flow table counts do not match expected 1, 1, 1, 1, 2, 1');
    END IF;
END;
/

PROMPT ==========================================
PROMPT 2. END-TO-END BUSINESS FLOW VALIDATION FOR 'dev01'
PROMPT ==========================================
SELECT 
    e.EMP_NAME,
    c.COURSE_NAME,
    req.STATUS AS REQ_STATUS,
    reg.STATUS AS REG_STATUS,
    att.SIGNIN_TYPE,
    rat.SCORE AS RATING_SCORE,
    cert.CERT_CODE
FROM EMPLOYEES e
JOIN TRAINING_COURSES c ON c.COURSE_NAME = 'C# Web API 最佳实践'
LEFT JOIN TRAINING_REQUESTS req ON req.EMP_ID = e.EMP_ID AND req.COURSE_ID = c.COURSE_ID
LEFT JOIN TRAINING_REGISTRATIONS reg ON reg.REQUEST_ID = req.REQUEST_ID
LEFT JOIN TRAINING_ATTENDANCE att ON att.REG_ID = reg.REG_ID
LEFT JOIN TRAINER_RATINGS rat ON rat.EMP_ID = e.EMP_ID AND rat.COURSE_ID = c.COURSE_ID
LEFT JOIN TRAINING_CERTIFICATES cert ON cert.EMP_ID = e.EMP_ID AND cert.COURSE_ID = c.COURSE_ID
WHERE e.LOGIN_NAME = 'dev01';

-- Assert the flow exists, was approved within dev01's own department, 
-- and is time-ordered (request <= approve <= file <= register;
--  pre <= start <= end <= post; sign-in <= end; certificate on/after the course end date).
DECLARE
    v_emp_dept      EMPLOYEES.DEPT_ID%TYPE;
    v_approver_dept EMPLOYEES.DEPT_ID%TYPE;
    v_start         TRAINING_COURSES.START_AT%TYPE;
    v_end           TRAINING_COURSES.END_AT%TYPE;
    v_req_status    TRAINING_REQUESTS.STATUS%TYPE;
    v_requested     TRAINING_REQUESTS.REQUESTED_AT%TYPE;
    v_approved      TRAINING_REQUESTS.DEPT_APPROVED_AT%TYPE;
    v_filed         TRAINING_REQUESTS.HR_FILED_AT%TYPE;
    v_reg_status    TRAINING_REGISTRATIONS.STATUS%TYPE;
    v_registered    TRAINING_REGISTRATIONS.REGISTERED_AT%TYPE;
    v_signin        TRAINING_ATTENDANCE.SIGNED_IN_AT%TYPE;
    v_pre           TRAINING_TESTS.TESTED_AT%TYPE;
    v_post          TRAINING_TESTS.TESTED_AT%TYPE;
    v_cert_date     TRAINING_CERTIFICATES.ISSUE_DATE%TYPE;
BEGIN
    SELECT e.DEPT_ID, ap.DEPT_ID, c.START_AT, c.END_AT,
           req.STATUS, req.REQUESTED_AT, req.DEPT_APPROVED_AT, req.HR_FILED_AT,
           reg.STATUS, reg.REGISTERED_AT, att.SIGNED_IN_AT, cert.ISSUE_DATE
      INTO v_emp_dept, v_approver_dept, v_start, v_end,
           v_req_status, v_requested, v_approved, v_filed,
           v_reg_status, v_registered, v_signin, v_cert_date
      FROM EMPLOYEES e
      JOIN TRAINING_COURSES c         ON c.COURSE_NAME = 'C# Web API 最佳实践'
      JOIN TRAINING_REQUESTS req      ON req.EMP_ID = e.EMP_ID AND req.COURSE_ID = c.COURSE_ID
      JOIN EMPLOYEES ap               ON ap.EMP_ID = req.DEPT_APPROVER_EMP_ID
      JOIN TRAINING_REGISTRATIONS reg ON reg.REQUEST_ID = req.REQUEST_ID
      JOIN TRAINING_ATTENDANCE att    ON att.REG_ID = reg.REG_ID
      JOIN TRAINING_CERTIFICATES cert ON cert.EMP_ID = e.EMP_ID AND cert.COURSE_ID = c.COURSE_ID
     WHERE e.LOGIN_NAME = 'dev01';

    SELECT MAX(CASE WHEN t.TEST_TYPE = 'PRE'  THEN t.TESTED_AT END),
           MAX(CASE WHEN t.TEST_TYPE = 'POST' THEN t.TESTED_AT END)
      INTO v_pre, v_post
      FROM TRAINING_TESTS t
      JOIN EMPLOYEES e        ON e.EMP_ID = t.EMP_ID
      JOIN TRAINING_COURSES c ON c.COURSE_ID = t.COURSE_ID
     WHERE e.LOGIN_NAME = 'dev01' AND c.COURSE_NAME = 'C# Web API 最佳实践';

    IF v_req_status <> 'HR_FILED' THEN
        RAISE_APPLICATION_ERROR(-20002, 'dev01 request not HR_FILED: ' || v_req_status);
    END IF;
    IF v_approver_dept <> v_emp_dept THEN
        RAISE_APPLICATION_ERROR(-20002, 'Approver and applicant are in different departments');
    END IF;
    IF v_reg_status <> 'COMPLETED' THEN
        RAISE_APPLICATION_ERROR(-20002, 'dev01 registration not COMPLETED: ' || v_reg_status);
    END IF;
    IF v_pre IS NULL OR v_post IS NULL THEN
        RAISE_APPLICATION_ERROR(-20002, 'dev01 missing PRE or POST test');
    END IF;
    IF NOT (v_requested <= v_approved AND v_approved <= v_filed
            AND v_filed <= v_registered
            AND v_pre <= v_start AND v_post >= v_end
            AND v_signin <= v_end
            AND v_cert_date >= TRUNC(CAST(v_end AS DATE))) THEN
        RAISE_APPLICATION_ERROR(-20002, 'dev01 flow timeline is out of order');
    END IF;
EXCEPTION
    WHEN NO_DATA_FOUND THEN
        RAISE_APPLICATION_ERROR(-20002, 'dev01 end-to-end flow incomplete (a stage row is missing)');
END;
/

PROMPT ==========================================
PROMPT 3. CONSTRAINT AUDIT: DUPLICATE ACTIVE REGISTRATIONS (EXPECTED: NO ROWS)
PROMPT ==========================================
SELECT EMP_ID, COURSE_ID, COUNT(*) AS DUPLICATE_COUNT
FROM TRAINING_REGISTRATIONS 
WHERE STATUS <> 'CANCELED' 
GROUP BY EMP_ID, COURSE_ID 
HAVING COUNT(*) > 1;

DECLARE
    v_cnt NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_cnt FROM (
        SELECT EMP_ID, COURSE_ID FROM TRAINING_REGISTRATIONS
        WHERE STATUS <> 'CANCELED'
        GROUP BY EMP_ID, COURSE_ID HAVING COUNT(*) > 1);
    IF v_cnt > 0 THEN
        RAISE_APPLICATION_ERROR(-20003, v_cnt || ' duplicate active registration group(s)');
    END IF;
END;
/

PROMPT ==========================================
PROMPT 4. CONSTRAINT AUDIT: BUDGET OVERDRAFT OR PUBLISH-SYNC MISMATCH (EXPECTED: NO ROWS)
PROMPT ==========================================
SELECT d.DEPT_ID, d.DEPT_NAME, d.ANNUAL_BUDGET, d.USED_BUDGET,
       (SELECT NVL(SUM(c.BUDGET_AMOUNT), 0) FROM TRAINING_COURSES c
        WHERE c.DEPT_ID = d.DEPT_ID AND c.COURSE_STATUS = 'PUBLISHED') AS PUBLISHED_TOTAL
FROM DEPARTMENTS_TRAINING d
WHERE d.USED_BUDGET < 0
   OR d.USED_BUDGET > d.ANNUAL_BUDGET
   OR d.USED_BUDGET <> (SELECT NVL(SUM(c.BUDGET_AMOUNT), 0) FROM TRAINING_COURSES c
                        WHERE c.DEPT_ID = d.DEPT_ID AND c.COURSE_STATUS = 'PUBLISHED');

DECLARE
    v_cnt NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_cnt FROM DEPARTMENTS_TRAINING d
    WHERE d.USED_BUDGET < 0
       OR d.USED_BUDGET > d.ANNUAL_BUDGET
       OR d.USED_BUDGET <> (SELECT NVL(SUM(c.BUDGET_AMOUNT), 0) FROM TRAINING_COURSES c
                            WHERE c.DEPT_ID = d.DEPT_ID AND c.COURSE_STATUS = 'PUBLISHED');
    IF v_cnt > 0 THEN
        RAISE_APPLICATION_ERROR(-20004, v_cnt || ' department(s) with budget overdraft or publish-sync mismatch');
    END IF;
END;
/

PROMPT ==========================================
PROMPT 5. CONSTRAINT AUDIT: TEST SCORE BOUNDARIES (EXPECTED: NO ROWS)
PROMPT ==========================================
SELECT TEST_ID, SCORE 
FROM TRAINING_TESTS 
WHERE SCORE < 0 OR SCORE > 100;

DECLARE
    v_cnt NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_cnt FROM TRAINING_TESTS WHERE SCORE < 0 OR SCORE > 100;
    IF v_cnt > 0 THEN
        RAISE_APPLICATION_ERROR(-20005, v_cnt || ' test score(s) out of [0,100]');
    END IF;
END;
/

PROMPT ==========================================
PROMPT 6. DATA INTEGRITY: ORPHANED REGISTRATIONS (EXPECTED: NO ROWS)
PROMPT ==========================================
SELECT REG_ID, REQUEST_ID 
FROM TRAINING_REGISTRATIONS 
WHERE REQUEST_ID NOT IN (SELECT REQUEST_ID FROM TRAINING_REQUESTS);

DECLARE
    v_cnt NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_cnt FROM TRAINING_REGISTRATIONS reg
    WHERE NOT EXISTS (SELECT 1 FROM TRAINING_REQUESTS q WHERE q.REQUEST_ID = reg.REQUEST_ID);
    IF v_cnt > 0 THEN
        RAISE_APPLICATION_ERROR(-20006, v_cnt || ' orphaned registration(s)');
    END IF;
END;
/

PROMPT ==========================================
PROMPT 7. STATE TRANSITION LOGIC: CERTIFICATE WITHOUT ATTENDANCE (EXPECTED: NO ROWS)
PROMPT ==========================================
-- Attendance links to a registration by REG_ID only, so a certificate is traced to attendance via its registration.
SELECT ct.CERT_ID, ct.EMP_ID, ct.COURSE_ID 
FROM TRAINING_CERTIFICATES ct
WHERE NOT EXISTS (
    SELECT 1 FROM TRAINING_REGISTRATIONS reg
    JOIN TRAINING_ATTENDANCE a ON a.REG_ID = reg.REG_ID
    WHERE reg.EMP_ID = ct.EMP_ID AND reg.COURSE_ID = ct.COURSE_ID);

DECLARE
    v_cnt NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_cnt FROM TRAINING_CERTIFICATES ct
    WHERE NOT EXISTS (
        SELECT 1 FROM TRAINING_REGISTRATIONS reg
        JOIN TRAINING_ATTENDANCE a ON a.REG_ID = reg.REG_ID
        WHERE reg.EMP_ID = ct.EMP_ID AND reg.COURSE_ID = ct.COURSE_ID);
    IF v_cnt > 0 THEN
        RAISE_APPLICATION_ERROR(-20007, v_cnt || ' certificate(s) without attendance');
    END IF;
END;
/

PROMPT ==========================================
PROMPT ALL VERIFICATION CHECKS PASSED
PROMPT ==========================================
