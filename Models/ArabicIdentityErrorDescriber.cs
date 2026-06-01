using Microsoft.AspNetCore.Identity;

namespace ParkingReservation.Models
{
    public class ArabicIdentityErrorDescriber : IdentityErrorDescriber
    {
        public override IdentityError DefaultError() => Error("حدث خطأ غير متوقع.");

        public override IdentityError ConcurrencyFailure() => Error("حدث تعارض أثناء حفظ البيانات، يرجى المحاولة مرة أخرى.");

        public override IdentityError PasswordMismatch() => Error("كلمة المرور غير صحيحة.");

        public override IdentityError InvalidToken() => Error("رمز التحقق غير صحيح.");

        public override IdentityError LoginAlreadyAssociated() => Error("يوجد مستخدم مرتبط بهذا الحساب مسبقاً.");

        public override IdentityError InvalidUserName(string? userName) => Error("اسم المستخدم غير صحيح.");

        public override IdentityError InvalidEmail(string? email) => Error("البريد الإلكتروني غير صحيح.");

        public override IdentityError DuplicateUserName(string userName) => Error("اسم المستخدم مستخدم مسبقاً.");

        public override IdentityError DuplicateEmail(string email) => Error("البريد الإلكتروني مستخدم مسبقاً.");

        public override IdentityError InvalidRoleName(string? role) => Error("اسم الدور غير صحيح.");

        public override IdentityError DuplicateRoleName(string role) => Error("اسم الدور مستخدم مسبقاً.");

        public override IdentityError UserAlreadyHasPassword() => Error("هذا المستخدم لديه كلمة مرور مسبقاً.");

        public override IdentityError UserLockoutNotEnabled() => Error("قفل الحساب غير مفعّل لهذا المستخدم.");

        public override IdentityError UserAlreadyInRole(string role) => Error("المستخدم موجود في هذا الدور مسبقاً.");

        public override IdentityError UserNotInRole(string role) => Error("المستخدم غير موجود في هذا الدور.");

        public override IdentityError PasswordTooShort(int length) => Error($"كلمة المرور يجب أن تكون {length} أحرف على الأقل.");

        public override IdentityError PasswordRequiresNonAlphanumeric() => Error("كلمة المرور يجب أن تحتوي على رمز واحد على الأقل.");

        public override IdentityError PasswordRequiresDigit() => Error("كلمة المرور يجب أن تحتوي على رقم واحد على الأقل.");

        public override IdentityError PasswordRequiresLower() => Error("كلمة المرور يجب أن تحتوي على حرف صغير واحد على الأقل.");

        public override IdentityError PasswordRequiresUpper() => Error("كلمة المرور يجب أن تحتوي على حرف كبير واحد على الأقل.");

        public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => Error($"كلمة المرور يجب أن تحتوي على {uniqueChars} أحرف مختلفة على الأقل.");

        public override IdentityError RecoveryCodeRedemptionFailed() => Error("رمز الاسترداد غير صحيح.");

        private static IdentityError Error(string description)
        {
            return new IdentityError { Description = description };
        }
    }
}
