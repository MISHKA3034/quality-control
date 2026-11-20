import { useEffect, useMemo, useState } from 'react';
import { useFieldArray, useForm, useWatch } from 'react-hook-form';
import { useNavigate } from 'react-router-dom';
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Divider,
  Grid,
  IconButton,
  MenuItem,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { Add as AddIcon, Delete as DeleteIcon } from '@mui/icons-material';
import { batchesApi, defectTypesApi, inspectionsApi } from '../api/endpoints';
import { getFieldErrors } from '../api/client';
import { PageHeader } from '../components/PageHeader';
import { useAuth } from '../contexts/AuthContext';
import { useNotification } from '../contexts/NotificationContext';
import type { DefectType, ProductionBatch } from '../types/api';
import { toInputDate } from '../utils/format';
import { defectSeverityLabels, inspectionOutcomeLabels } from '../utils/labels';

interface ResultLine {
  parameterName: string;
  expectedValue: string;
  actualValue: string;
  unitOfMeasure: string;
  outcome: 'Conforming' | 'ReworkRequired' | 'Defective';
  comment: string;
}

interface DefectLine {
  defectTypeId: number | '';
  description: string;
  severity: 'Minor' | 'Major' | 'Critical';
  location: string;
  comment: string;
  affectedQuantity: number | '';
}

interface InspectionForm {
  productionBatchId: number | '';
  inspectionDate: string;
  outcome: 'Conforming' | 'ReworkRequired' | 'Defective';
  comment: string;
  quantityInspected: number | '';
  quantityAccepted: number | '';
  quantityRejected: number | '';
  results: ResultLine[];
  defects: DefectLine[];
}

export function InspectionCreatePage() {
  const navigate = useNavigate();
  const { user } = useAuth();
  const { notifySuccess, notifyError } = useNotification();
  const [batches, setBatches] = useState<ProductionBatch[]>([]);
  const [defectTypes, setDefectTypes] = useState<DefectType[]>([]);

  const {
    register,
    handleSubmit,
    control,
    setValue,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<InspectionForm>({
    defaultValues: {
      productionBatchId: '',
      inspectionDate: toInputDate(new Date()),
      outcome: 'Conforming',
      comment: '',
      quantityInspected: '',
      quantityAccepted: '',
      quantityRejected: '',
      results: [
        {
          parameterName: '',
          expectedValue: '',
          actualValue: '',
          unitOfMeasure: '',
          outcome: 'Conforming',
          comment: '',
        },
      ],
      defects: [],
    },
  });

  const results = useFieldArray({ control, name: 'results' });
  const defects = useFieldArray({ control, name: 'defects' });

  const outcome = useWatch({ control, name: 'outcome' });
  const batchId = useWatch({ control, name: 'productionBatchId' });
  const inspected = useWatch({ control, name: 'quantityInspected' });
  const rejected = useWatch({ control, name: 'quantityRejected' });

  useEffect(() => {
    batchesApi.getInspectable().then(setBatches).catch(notifyError);
    defectTypesApi.lookup().then(setDefectTypes).catch(notifyError);
  }, [notifyError]);

  const selectedBatch = useMemo(
    () => batches.find((b) => b.id === Number(batchId)),
    [batches, batchId],
  );

  // Accepted is always inspected minus rejected — deriving it removes a whole
  // class of arithmetic mistakes the server would reject anyway.
  useEffect(() => {
    const total = Number(inspected);
    const bad = Number(rejected);

    if (Number.isFinite(total) && Number.isFinite(bad) && total >= bad) {
      setValue('quantityAccepted', total - bad);
    }
  }, [inspected, rejected, setValue]);

  // The server refuses "Conforming" together with defects; mirror that here so
  // the contradiction is impossible to build rather than rejected after the fact.
  useEffect(() => {
    if (outcome === 'Conforming') {
      defects.replace([]);
      setValue('quantityRejected', 0);
    } else if (defects.fields.length === 0) {
      defects.append({
        defectTypeId: '',
        description: '',
        severity: 'Major',
        location: '',
        comment: '',
        affectedQuantity: 1,
      });
    }
    // defects.fields.length is intentionally not a dependency: appending would
    // retrigger the effect and append forever.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [outcome]);

  async function onSubmit(form: InspectionForm) {
    try {
      const inspection = await inspectionsApi.create({
        productionBatchId: Number(form.productionBatchId),
        inspectionDate: new Date(form.inspectionDate).toISOString(),
        outcome: form.outcome,
        comment: form.comment || undefined,
        quantityInspected: Number(form.quantityInspected),
        quantityAccepted: Number(form.quantityAccepted),
        quantityRejected: Number(form.quantityRejected),
        results: form.results
          .filter((r) => r.parameterName.trim() !== '')
          .map((r, index) => ({
            parameterName: r.parameterName,
            expectedValue: r.expectedValue || undefined,
            actualValue: r.actualValue || undefined,
            unitOfMeasure: r.unitOfMeasure || undefined,
            outcome: r.outcome,
            comment: r.comment || undefined,
            sortOrder: index + 1,
          })),
        defects: form.defects.map((d) => ({
          defectTypeId: Number(d.defectTypeId),
          description: d.description,
          severity: d.severity,
          location: d.location || undefined,
          comment: d.comment || undefined,
          affectedQuantity: Number(d.affectedQuantity),
        })),
      });

      notifySuccess(`Акт ${inspection.inspectionNumber} оформлен.`);
      navigate(`/inspections/${inspection.id}`);
    } catch (error) {
      const fieldErrors = getFieldErrors(error);
      let handled = false;

      for (const [field, message] of Object.entries(fieldErrors)) {
        setError(field as keyof InspectionForm, { message });
        handled = true;
      }

      if (!handled) {
        notifyError(error);
      }
    }
  }

  // An act is signed by an employee. Accounts without one cannot file it, and the
  // server rejects the attempt — say so before the form is filled in.
  if (user && !user.employeeId) {
    return (
      <>
        <PageHeader title="Оформление акта проверки" />
        <Alert severity="warning">
          Ваша учётная запись не связана с сотрудником, поэтому подписать акт проверки нельзя.
          Обратитесь к администратору системы.
        </Alert>
      </>
    );
  }

  return (
    <>
      <PageHeader
        title="Оформление акта проверки"
        subtitle={`Контролёр: ${user?.fullName ?? user?.userName ?? ''}`}
      />

      <form onSubmit={handleSubmit(onSubmit)} noValidate>
        <Card sx={{ mb: 2 }}>
          <CardContent>
            <Typography variant="h6" gutterBottom>
              Партия и результат
            </Typography>

            <Grid container spacing={2}>
              <Grid item xs={12} md={6}>
                <TextField
                  select
                  label="Партия"
                  fullWidth
                  required
                  defaultValue=""
                  error={Boolean(errors.productionBatchId)}
                  helperText={errors.productionBatchId?.message}
                  {...register('productionBatchId', { required: 'Выберите партию.' })}
                >
                  {batches.map((b) => (
                    <MenuItem key={b.id} value={b.id}>
                      {b.batchNumber} — {b.productName} ({b.quantity} шт., {b.departmentName})
                    </MenuItem>
                  ))}
                </TextField>
              </Grid>

              <Grid item xs={12} md={3}>
                <TextField
                  type="date"
                  label="Дата проверки"
                  fullWidth
                  required
                  InputLabelProps={{ shrink: true }}
                  error={Boolean(errors.inspectionDate)}
                  helperText={errors.inspectionDate?.message}
                  {...register('inspectionDate', { required: 'Укажите дату.' })}
                />
              </Grid>

              <Grid item xs={12} md={3}>
                <TextField
                  select
                  label="Результат проверки"
                  fullWidth
                  required
                  defaultValue="Conforming"
                  error={Boolean(errors.outcome)}
                  helperText={errors.outcome?.message}
                  {...register('outcome', { required: true })}
                >
                  {Object.entries(inspectionOutcomeLabels).map(([value, label]) => (
                    <MenuItem key={value} value={value}>
                      {label}
                    </MenuItem>
                  ))}
                </TextField>
              </Grid>

              <Grid item xs={12} sm={4}>
                <TextField
                  type="number"
                  label="Проверено, шт."
                  fullWidth
                  required
                  inputProps={{ min: 1, max: selectedBatch?.quantity }}
                  error={Boolean(errors.quantityInspected)}
                  helperText={
                    errors.quantityInspected?.message ??
                    (selectedBatch ? `В партии ${selectedBatch.quantity} шт.` : ' ')
                  }
                  {...register('quantityInspected', {
                    required: 'Укажите количество проверенных изделий.',
                    min: { value: 1, message: 'Должно быть больше нуля.' },
                    max: selectedBatch
                      ? { value: selectedBatch.quantity, message: 'Больше, чем в партии.' }
                      : undefined,
                  })}
                />
              </Grid>

              <Grid item xs={12} sm={4}>
                <TextField
                  type="number"
                  label="Забраковано, шт."
                  fullWidth
                  required
                  inputProps={{ min: 0 }}
                  disabled={outcome === 'Conforming'}
                  error={Boolean(errors.quantityRejected)}
                  helperText={
                    errors.quantityRejected?.message ??
                    (outcome === 'Conforming' ? 'При результате «Соответствует» — 0.' : ' ')
                  }
                  {...register('quantityRejected', {
                    required: 'Укажите количество.',
                    min: { value: 0, message: 'Не может быть отрицательным.' },
                  })}
                />
              </Grid>

              <Grid item xs={12} sm={4}>
                <TextField
                  type="number"
                  label="Принято, шт."
                  fullWidth
                  // Computed from the other two; editing it could only create a
                  // contradiction.
                  InputProps={{ readOnly: true }}
                  helperText="Рассчитывается автоматически"
                  {...register('quantityAccepted')}
                />
              </Grid>

              <Grid item xs={12}>
                <TextField label="Комментарий к акту" fullWidth multiline rows={2} {...register('comment')} />
              </Grid>
            </Grid>
          </CardContent>
        </Card>

        <Card sx={{ mb: 2 }}>
          <CardContent>
            <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: 2 }}>
              <Typography variant="h6">Контролируемые параметры</Typography>

              <Button
                size="small"
                startIcon={<AddIcon />}
                onClick={() =>
                  results.append({
                    parameterName: '',
                    expectedValue: '',
                    actualValue: '',
                    unitOfMeasure: '',
                    outcome: 'Conforming',
                    comment: '',
                  })
                }
              >
                Добавить параметр
              </Button>
            </Stack>

            {results.fields.map((field, index) => (
              <Box key={field.id}>
                {index > 0 ? <Divider sx={{ my: 2 }} /> : null}

                <Grid container spacing={1.5} alignItems="flex-start">
                  <Grid item xs={12} md={3}>
                    <TextField
                      label="Параметр"
                      fullWidth
                      placeholder="Диаметр отверстия Ø20H7"
                      {...register(`results.${index}.parameterName` as const)}
                    />
                  </Grid>

                  <Grid item xs={6} md={2}>
                    <TextField
                      label="Требование"
                      fullWidth
                      placeholder="20,0…20,021"
                      {...register(`results.${index}.expectedValue` as const)}
                    />
                  </Grid>

                  <Grid item xs={6} md={2}>
                    <TextField
                      label="Факт"
                      fullWidth
                      placeholder="20,015"
                      {...register(`results.${index}.actualValue` as const)}
                    />
                  </Grid>

                  <Grid item xs={4} md={1}>
                    <TextField label="Ед." fullWidth {...register(`results.${index}.unitOfMeasure` as const)} />
                  </Grid>

                  <Grid item xs={8} md={2.5}>
                    <TextField
                      select
                      label="Вердикт"
                      fullWidth
                      defaultValue="Conforming"
                      {...register(`results.${index}.outcome` as const)}
                    >
                      {Object.entries(inspectionOutcomeLabels).map(([value, label]) => (
                        <MenuItem key={value} value={value}>
                          {label}
                        </MenuItem>
                      ))}
                    </TextField>
                  </Grid>

                  <Grid item xs={12} md={1.5} sx={{ textAlign: 'right' }}>
                    <IconButton
                      color="error"
                      // The last line cannot be removed: an act with no parameters
                      // records nothing.
                      disabled={results.fields.length === 1}
                      onClick={() => results.remove(index)}
                    >
                      <DeleteIcon />
                    </IconButton>
                  </Grid>
                </Grid>
              </Box>
            ))}
          </CardContent>
        </Card>

        {outcome !== 'Conforming' ? (
          <Card sx={{ mb: 2 }}>
            <CardContent>
              <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: 2 }}>
                <Typography variant="h6">Дефекты</Typography>

                <Button
                  size="small"
                  startIcon={<AddIcon />}
                  onClick={() =>
                    defects.append({
                      defectTypeId: '',
                      description: '',
                      severity: 'Major',
                      location: '',
                      comment: '',
                      affectedQuantity: 1,
                    })
                  }
                >
                  Добавить дефект
                </Button>
              </Stack>

              <Alert severity="info" sx={{ mb: 2 }}>
                При результате «Требуется доработка» или «Брак» необходимо указать хотя бы один дефект.
                Фотографии прикрепляются после сохранения акта — в карточке дефекта.
              </Alert>

              {defects.fields.map((field, index) => (
                <Box key={field.id}>
                  {index > 0 ? <Divider sx={{ my: 2 }} /> : null}

                  <Grid container spacing={1.5}>
                    <Grid item xs={12} md={3}>
                      <TextField
                        select
                        label="Вид дефекта"
                        fullWidth
                        required
                        defaultValue=""
                        {...register(`defects.${index}.defectTypeId` as const, { required: true })}
                        onChange={(e) => {
                          // Pre-fills the severity from the classifier, which is
                          // right most of the time and still overridable.
                          const type = defectTypes.find((t) => t.id === Number(e.target.value));
                          setValue(`defects.${index}.defectTypeId`, Number(e.target.value));

                          if (type) {
                            setValue(`defects.${index}.severity`, type.defaultSeverity);
                          }
                        }}
                      >
                        {defectTypes.map((t) => (
                          <MenuItem key={t.id} value={t.id}>
                            {t.code} — {t.name}
                          </MenuItem>
                        ))}
                      </TextField>
                    </Grid>

                    <Grid item xs={12} md={4}>
                      <TextField
                        label="Описание дефекта"
                        fullWidth
                        required
                        {...register(`defects.${index}.description` as const, { required: true })}
                      />
                    </Grid>

                    <Grid item xs={6} md={2}>
                      <TextField
                        select
                        label="Критичность"
                        fullWidth
                        defaultValue="Major"
                        {...register(`defects.${index}.severity` as const)}
                      >
                        {Object.entries(defectSeverityLabels).map(([value, label]) => (
                          <MenuItem key={value} value={value}>
                            {label}
                          </MenuItem>
                        ))}
                      </TextField>
                    </Grid>

                    <Grid item xs={6} md={2}>
                      <TextField
                        label="Место"
                        fullWidth
                        placeholder="Шов Ш-3"
                        {...register(`defects.${index}.location` as const)}
                      />
                    </Grid>

                    <Grid item xs={6} md={1}>
                      <TextField
                        type="number"
                        label="Кол-во"
                        fullWidth
                        inputProps={{ min: 1 }}
                        {...register(`defects.${index}.affectedQuantity` as const, { min: 1 })}
                      />
                    </Grid>

                    <Grid item xs={12} sx={{ textAlign: 'right' }}>
                      <Button
                        size="small"
                        color="error"
                        startIcon={<DeleteIcon />}
                        disabled={defects.fields.length === 1}
                        onClick={() => defects.remove(index)}
                      >
                        Удалить дефект
                      </Button>
                    </Grid>
                  </Grid>
                </Box>
              ))}
            </CardContent>
          </Card>
        ) : null}

        <Stack direction="row" spacing={2} justifyContent="flex-end">
          <Button onClick={() => navigate('/inspections')}>Отмена</Button>
          <Button type="submit" variant="contained" size="large" disabled={isSubmitting}>
            {isSubmitting ? 'Сохранение…' : 'Оформить акт'}
          </Button>
        </Stack>
      </form>
    </>
  );
}
