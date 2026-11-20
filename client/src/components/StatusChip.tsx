import { Chip } from '@mui/material';
import type { ChipProps } from '@mui/material';
import type {
  BatchStatus,
  CorrectiveActionStatus,
  DefectSeverity,
  InspectionOutcome,
} from '../types/api';
import {
  batchStatusColours,
  batchStatusLabels,
  correctiveActionStatusColours,
  correctiveActionStatusLabels,
  defectSeverityColours,
  defectSeverityLabels,
  inspectionOutcomeColours,
  inspectionOutcomeLabels,
} from '../utils/labels';

type Size = ChipProps['size'];

/**
 * Status chips.
 *
 * One component per enum rather than a generic one: each keeps its label and
 * colour maps together, so a status can never be rendered with another's palette.
 */

export function BatchStatusChip({ status, size = 'small' }: { status: BatchStatus; size?: Size }) {
  return <Chip label={batchStatusLabels[status]} color={batchStatusColours[status]} size={size} />;
}

export function InspectionOutcomeChip({
  outcome,
  size = 'small',
}: {
  outcome: InspectionOutcome;
  size?: Size;
}) {
  return (
    <Chip
      label={inspectionOutcomeLabels[outcome]}
      color={inspectionOutcomeColours[outcome]}
      size={size}
    />
  );
}

export function DefectSeverityChip({
  severity,
  size = 'small',
}: {
  severity: DefectSeverity;
  size?: Size;
}) {
  return (
    <Chip
      label={defectSeverityLabels[severity]}
      color={defectSeverityColours[severity]}
      size={size}
      // Critical defects are the whole point of the system; make them impossible
      // to skim past.
      variant={severity === 'Critical' ? 'filled' : 'outlined'}
    />
  );
}

export function CorrectiveActionStatusChip({
  status,
  size = 'small',
}: {
  status: CorrectiveActionStatus;
  size?: Size;
}) {
  return (
    <Chip
      label={correctiveActionStatusLabels[status]}
      color={correctiveActionStatusColours[status]}
      size={size}
    />
  );
}
