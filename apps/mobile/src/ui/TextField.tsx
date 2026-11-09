import { useState } from 'react';
import { Pressable, StyleSheet, TextInput, View, type TextInputProps } from 'react-native';

import { colors, fonts, minTouchTarget, radii, spacing } from '@/theme/tokens';

import { AppText } from './AppText';
import { CircleAlert, Eye, EyeOff, iconDefaults } from './icons';

export type TextFieldProps = Omit<TextInputProps, 'secureTextEntry' | 'style'> & {
  label: string;
  /** Shown under the field in red; also marks the field invalid. */
  error?: string;
  /** Neutral help shown under the field when there is no error. */
  hint?: string;
  /** Password field with a show / hide button. */
  secure?: boolean;
};

export function TextField({ label, error, hint, secure = false, ...inputProps }: TextFieldProps) {
  const [revealed, setRevealed] = useState(false);
  const message = error ?? hint;

  return (
    <View style={styles.field}>
      <AppText variant="label" importantForAccessibility="no">
        {label}
      </AppText>
      <View style={styles.inputRow}>
        <TextInput
          {...inputProps}
          accessibilityLabel={label}
          accessibilityHint={message}
          aria-invalid={error ? true : undefined}
          secureTextEntry={secure && !revealed}
          placeholderTextColor={colors.muted}
          style={[
            styles.input,
            error ? styles.inputError : null,
            secure ? styles.inputSecure : null,
          ]}
        />
        {secure ? (
          <Pressable
            accessibilityRole="button"
            accessibilityLabel={revealed ? 'Masquer le mot de passe' : 'Afficher le mot de passe'}
            onPress={() => setRevealed((value) => !value)}
            style={styles.eye}
            hitSlop={4}
          >
            {revealed ? (
              <EyeOff color={colors.muted} {...iconDefaults} />
            ) : (
              <Eye color={colors.muted} {...iconDefaults} />
            )}
          </Pressable>
        ) : null}
      </View>
      {error ? (
        <View style={styles.message} accessibilityLiveRegion="polite">
          <CircleAlert color={colors.danger} size={16} strokeWidth={iconDefaults.strokeWidth} />
          <AppText variant="caption" color={colors.danger} style={styles.messageText}>
            {error}
          </AppText>
        </View>
      ) : hint ? (
        <AppText variant="caption" color={colors.muted}>
          {hint}
        </AppText>
      ) : null}
    </View>
  );
}

const styles = StyleSheet.create({
  field: { gap: spacing.sm },
  inputRow: { justifyContent: 'center' },
  input: {
    height: 56,
    borderWidth: 1.5,
    borderColor: colors.border,
    borderRadius: radii.lg,
    paddingHorizontal: spacing.lg,
    backgroundColor: colors.surface,
    color: colors.ink,
    fontFamily: fonts.regular,
    fontSize: 16,
  },
  inputError: { borderWidth: 2, borderColor: colors.danger },
  inputSecure: { paddingRight: 56 },
  eye: {
    position: 'absolute',
    right: 6,
    width: minTouchTarget,
    height: minTouchTarget,
    alignItems: 'center',
    justifyContent: 'center',
  },
  message: { flexDirection: 'row', gap: 6, alignItems: 'flex-start' },
  messageText: { flex: 1 },
});
