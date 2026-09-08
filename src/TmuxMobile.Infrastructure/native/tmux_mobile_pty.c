#if defined(__APPLE__)
#include <util.h>
#else
#include <pty.h>
#endif

#include <sys/ioctl.h>
#include <stdlib.h>
#include <unistd.h>

int tmux_mobile_forkpty_exec(
    int *master_fd,
    const struct winsize *window_size,
    const char *executable,
    char *const arguments[])
{
    /* forkpty takes a non-const winsize on some platforms, so copy it. */
    struct winsize size = *window_size;
    pid_t process_id = forkpty(master_fd, NULL, NULL, &size);
    if (process_id != 0)
        return process_id;

    execv(executable, arguments);
    _exit(127);
}

/*
 * ioctl(2) is variadic: int ioctl(int, unsigned long, ...).
 *
 * On arm64 Apple platforms the ABI passes variadic arguments on the stack
 * rather than in registers, so a P/Invoke that declares ioctl as a fixed-arity
 * function puts the winsize pointer in a register the callee never reads. The
 * kernel then copies whatever the stack happened to hold, which silently
 * corrupts the terminal size instead of failing. Making the call from C lets
 * the compiler emit the correct calling sequence, and keeps the value of
 * TIOCSWINSZ (which differs per platform) out of the managed side.
 */
int tmux_mobile_set_winsize(int fd, const struct winsize *window_size)
{
    return ioctl(fd, TIOCSWINSZ, window_size);
}
